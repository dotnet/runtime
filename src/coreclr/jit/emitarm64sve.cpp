// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XX                                                                           XX
XX                             emitArm64sve.cpp                              XX
XX                                                                           XX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
*/

#include "jitpch.h"
#ifdef _MSC_VER
#pragma hdrstop
#endif

#ifdef TARGET_ARM64

/*****************************************************************************/
/*****************************************************************************/

#include "instr.h"

/*****************************************************************************/

// clang-format off
static const char * const  zRegNames[] =
{
    "z0",  "z1",  "z2",  "z3",  "z4",
    "z5",  "z6",  "z7",  "z8",  "z9",
    "z10", "z11", "z12", "z13", "z14",
    "z15", "z16", "z17", "z18", "z19",
    "z20", "z21", "z22", "z23", "z24",
    "z25", "z26", "z27", "z28", "z29",
    "z30", "z31"
};

static const char * const  pRegNames[] =
{
    "p0",  "p1",  "p2",  "p3",  "p4",
    "p5",  "p6",  "p7",  "p8",  "p9",
    "p10", "p11", "p12", "p13", "p14",
    "p15"
};

static const char * const  pnRegNames[] =
{
    "pn0",  "pn1",  "pn2",  "pn3",  "pn4",
    "pn5",  "pn6",  "pn7",  "pn8",  "pn9",
    "pn10", "pn11", "pn12", "pn13", "pn14",
    "pn15"
};

static const char * const  svePatternNames[] =
{
    "pow2", "vl1", "vl2", "vl3",
    "vl4", "vl5", "vl6", "vl7",
    "vl8", "vl16", "vl32", "vl64",
    "vl128", "vl256", "invalid", "invalid",
    "invalid", "invalid", "invalid", "invalid",
    "invalid", "invalid", "invalid", "invalid",
    "invalid", "invalid", "invalid", "invalid",
    "invalid", "mul4", "mul3", "all"
};

// clang-format on

/*****************************************************************************
 *
 *  Returns the specific encoding of the given CPU instruction and format
 */

emitter::code_t emitter::emitInsCodeSve(instruction ins, insFormat fmt)
{
    // clang-format off
    const static code_t insCodes1[] =
    {
        #define   INST1(id, nm, info, fmt, e1                                                       ) e1,
        #define   INST2(id, nm, info, fmt, e1, e2                                                   ) e1,
        #define   INST3(id, nm, info, fmt, e1, e2, e3                                               ) e1,
        #define   INST4(id, nm, info, fmt, e1, e2, e3, e4                                           ) e1,
        #define   INST5(id, nm, info, fmt, e1, e2, e3, e4, e5                                       ) e1,
        #define   INST6(id, nm, info, fmt, e1, e2, e3, e4, e5, e6                                   ) e1,
        #define   INST7(id, nm, info, fmt, e1, e2, e3, e4, e5, e6, e7                               ) e1,
        #define   INST8(id, nm, info, fmt, e1, e2, e3, e4, e5, e6, e7, e8                           ) e1,
        #define   INST9(id, nm, info, fmt, e1, e2, e3, e4, e5, e6, e7, e8, e9                       ) e1,
        #define  INST11(id, nm, info, fmt, e1, e2, e3, e4, e5, e6, e7, e8, e9, e10, e11             ) e1,
        #define  INST13(id, nm, info, fmt, e1, e2, e3, e4, e5, e6, e7, e8, e9, e10, e11, e12, e13   ) e1,
        #include "instrsarm64sve.h"
    };

    const static code_t insCodes2[] =
    {
        #define   INST1(id, nm, info, fmt, e1                                                       ) 
        #define   INST2(id, nm, info, fmt, e1, e2                                                   ) e2,
        #define   INST3(id, nm, info, fmt, e1, e2, e3                                               ) e2,
        #define   INST4(id, nm, info, fmt, e1, e2, e3, e4                                           ) e2,
        #define   INST5(id, nm, info, fmt, e1, e2, e3, e4, e5                                       ) e2,
        #define   INST6(id, nm, info, fmt, e1, e2, e3, e4, e5, e6                                   ) e2,
        #define   INST7(id, nm, info, fmt, e1, e2, e3, e4, e5, e6, e7                               ) e2,
        #define   INST8(id, nm, info, fmt, e1, e2, e3, e4, e5, e6, e7, e8                           ) e2,
        #define   INST9(id, nm, info, fmt, e1, e2, e3, e4, e5, e6, e7, e8, e9                       ) e2,
        #define  INST11(id, nm, info, fmt, e1, e2, e3, e4, e5, e6, e7, e8, e9, e10, e11             ) e2,
        #define  INST13(id, nm, info, fmt, e1, e2, e3, e4, e5, e6, e7, e8, e9, e10, e11, e12, e13   ) e2,
        #include "instrsarm64sve.h"
    };

    const static code_t insCodes3[] =
    {
        #define   INST1(id, nm, info, fmt, e1                                                       ) 
        #define   INST2(id, nm, info, fmt, e1, e2                                                   ) 
        #define   INST3(id, nm, info, fmt, e1, e2, e3                                               ) e3,
        #define   INST4(id, nm, info, fmt, e1, e2, e3, e4                                           ) e3,
        #define   INST5(id, nm, info, fmt, e1, e2, e3, e4, e5                                       ) e3,
        #define   INST6(id, nm, info, fmt, e1, e2, e3, e4, e5, e6                                   ) e3,
        #define   INST7(id, nm, info, fmt, e1, e2, e3, e4, e5, e6, e7                               ) e3,
        #define   INST8(id, nm, info, fmt, e1, e2, e3, e4, e5, e6, e7, e8                           ) e3,
        #define   INST9(id, nm, info, fmt, e1, e2, e3, e4, e5, e6, e7, e8, e9                       ) e3,
        #define  INST11(id, nm, info, fmt, e1, e2, e3, e4, e5, e6, e7, e8, e9, e10, e11             ) e3,
        #define  INST13(id, nm, info, fmt, e1, e2, e3, e4, e5, e6, e7, e8, e9, e10, e11, e12, e13   ) e3,
        #include "instrsarm64sve.h"
    };

    const static code_t insCodes4[] =
    {
        #define   INST1(id, nm, info, fmt, e1                                                       ) 
        #define   INST2(id, nm, info, fmt, e1, e2                                                   ) 
        #define   INST3(id, nm, info, fmt, e1, e2, e3                                               ) 
        #define   INST4(id, nm, info, fmt, e1, e2, e3, e4                                           ) e4,
        #define   INST5(id, nm, info, fmt, e1, e2, e3, e4, e5                                       ) e4,
        #define   INST6(id, nm, info, fmt, e1, e2, e3, e4, e5, e6                                   ) e4,
        #define   INST7(id, nm, info, fmt, e1, e2, e3, e4, e5, e6, e7                               ) e4,
        #define   INST8(id, nm, info, fmt, e1, e2, e3, e4, e5, e6, e7, e8                           ) e4,
        #define   INST9(id, nm, info, fmt, e1, e2, e3, e4, e5, e6, e7, e8, e9                       ) e4,
        #define  INST11(id, nm, info, fmt, e1, e2, e3, e4, e5, e6, e7, e8, e9, e10, e11             ) e4,
        #define  INST13(id, nm, info, fmt, e1, e2, e3, e4, e5, e6, e7, e8, e9, e10, e11, e12, e13   ) e4,
        #include "instrsarm64sve.h"
    };

    const static code_t insCodes5[] =
    {
        #define   INST1(id, nm, info, fmt, e1                                                       ) 
        #define   INST2(id, nm, info, fmt, e1, e2                                                   ) 
        #define   INST3(id, nm, info, fmt, e1, e2, e3                                               ) 
        #define   INST4(id, nm, info, fmt, e1, e2, e3, e4                                           ) 
        #define   INST5(id, nm, info, fmt, e1, e2, e3, e4, e5                                       ) e5,
        #define   INST6(id, nm, info, fmt, e1, e2, e3, e4, e5, e6                                   ) e5,
        #define   INST7(id, nm, info, fmt, e1, e2, e3, e4, e5, e6, e7                               ) e5,
        #define   INST8(id, nm, info, fmt, e1, e2, e3, e4, e5, e6, e7, e8                           ) e5,
        #define   INST9(id, nm, info, fmt, e1, e2, e3, e4, e5, e6, e7, e8, e9                       ) e5,
        #define  INST11(id, nm, info, fmt, e1, e2, e3, e4, e5, e6, e7, e8, e9, e10, e11             ) e5,
        #define  INST13(id, nm, info, fmt, e1, e2, e3, e4, e5, e6, e7, e8, e9, e10, e11, e12, e13   ) e5,
        #include "instrsarm64sve.h"
    };

    const static code_t insCodes6[] =
    {
        #define   INST1(id, nm, info, fmt, e1                                                       ) 
        #define   INST2(id, nm, info, fmt, e1, e2                                                   ) 
        #define   INST3(id, nm, info, fmt, e1, e2, e3                                               ) 
        #define   INST4(id, nm, info, fmt, e1, e2, e3, e4                                           ) 
        #define   INST5(id, nm, info, fmt, e1, e2, e3, e4, e5                                       ) 
        #define   INST6(id, nm, info, fmt, e1, e2, e3, e4, e5, e6                                   ) e6,
        #define   INST7(id, nm, info, fmt, e1, e2, e3, e4, e5, e6, e7                               ) e6,
        #define   INST8(id, nm, info, fmt, e1, e2, e3, e4, e5, e6, e7, e8                           ) e6,
        #define   INST9(id, nm, info, fmt, e1, e2, e3, e4, e5, e6, e7, e8, e9                       ) e6,
        #define  INST11(id, nm, info, fmt, e1, e2, e3, e4, e5, e6, e7, e8, e9, e10, e11             ) e6,
        #define  INST13(id, nm, info, fmt, e1, e2, e3, e4, e5, e6, e7, e8, e9, e10, e11, e12, e13   ) e6,
        #include "instrsarm64sve.h"
    };

    const static code_t insCodes7[] =
    {
        #define   INST1(id, nm, info, fmt, e1                                                       ) 
        #define   INST2(id, nm, info, fmt, e1, e2                                                   ) 
        #define   INST3(id, nm, info, fmt, e1, e2, e3                                               ) 
        #define   INST4(id, nm, info, fmt, e1, e2, e3, e4                                           ) 
        #define   INST5(id, nm, info, fmt, e1, e2, e3, e4, e5                                       ) 
        #define   INST6(id, nm, info, fmt, e1, e2, e3, e4, e5, e6                                   ) 
        #define   INST7(id, nm, info, fmt, e1, e2, e3, e4, e5, e6, e7                               ) e7,
        #define   INST8(id, nm, info, fmt, e1, e2, e3, e4, e5, e6, e7, e8                           ) e7,
        #define   INST9(id, nm, info, fmt, e1, e2, e3, e4, e5, e6, e7, e8, e9                       ) e7,
        #define  INST11(id, nm, info, fmt, e1, e2, e3, e4, e5, e6, e7, e8, e9, e10, e11             ) e7,
        #define  INST13(id, nm, info, fmt, e1, e2, e3, e4, e5, e6, e7, e8, e9, e10, e11, e12, e13   ) e7,
        #include "instrsarm64sve.h"
    };

    const static code_t insCodes8[] =
    {
        #define   INST1(id, nm, info, fmt, e1                                                       ) 
        #define   INST2(id, nm, info, fmt, e1, e2                                                   ) 
        #define   INST3(id, nm, info, fmt, e1, e2, e3                                               ) 
        #define   INST4(id, nm, info, fmt, e1, e2, e3, e4                                           ) 
        #define   INST5(id, nm, info, fmt, e1, e2, e3, e4, e5                                       ) 
        #define   INST6(id, nm, info, fmt, e1, e2, e3, e4, e5, e6                                   ) 
        #define   INST7(id, nm, info, fmt, e1, e2, e3, e4, e5, e6, e7                               ) 
        #define   INST8(id, nm, info, fmt, e1, e2, e3, e4, e5, e6, e7, e8                           ) e8,
        #define   INST9(id, nm, info, fmt, e1, e2, e3, e4, e5, e6, e7, e8, e9                       ) e8,
        #define  INST11(id, nm, info, fmt, e1, e2, e3, e4, e5, e6, e7, e8, e9, e10, e11             ) e8,
        #define  INST13(id, nm, info, fmt, e1, e2, e3, e4, e5, e6, e7, e8, e9, e10, e11, e12, e13   ) e8,
        #include "instrsarm64sve.h"
    };

    const static code_t insCodes9[] =
    {
        #define   INST1(id, nm, info, fmt, e1                                                       ) 
        #define   INST2(id, nm, info, fmt, e1, e2                                                   ) 
        #define   INST3(id, nm, info, fmt, e1, e2, e3                                               ) 
        #define   INST4(id, nm, info, fmt, e1, e2, e3, e4                                           ) 
        #define   INST5(id, nm, info, fmt, e1, e2, e3, e4, e5                                       ) 
        #define   INST6(id, nm, info, fmt, e1, e2, e3, e4, e5, e6                                   ) 
        #define   INST7(id, nm, info, fmt, e1, e2, e3, e4, e5, e6, e7                               ) 
        #define   INST8(id, nm, info, fmt, e1, e2, e3, e4, e5, e6, e7, e8                           ) 
        #define   INST9(id, nm, info, fmt, e1, e2, e3, e4, e5, e6, e7, e8, e9                       ) e9,
        #define  INST11(id, nm, info, fmt, e1, e2, e3, e4, e5, e6, e7, e8, e9, e10, e11             ) e9,
        #define  INST13(id, nm, info, fmt, e1, e2, e3, e4, e5, e6, e7, e8, e9, e10, e11, e12, e13   ) e9,
        #include "instrsarm64sve.h"
    };

    const static code_t insCodes10[] =
    {
        #define   INST1(id, nm, info, fmt, e1                                                       ) 
        #define   INST2(id, nm, info, fmt, e1, e2                                                   ) 
        #define   INST3(id, nm, info, fmt, e1, e2, e3                                               ) 
        #define   INST4(id, nm, info, fmt, e1, e2, e3, e4                                           ) 
        #define   INST5(id, nm, info, fmt, e1, e2, e3, e4, e5                                       ) 
        #define   INST6(id, nm, info, fmt, e1, e2, e3, e4, e5, e6                                   ) 
        #define   INST7(id, nm, info, fmt, e1, e2, e3, e4, e5, e6, e7                               ) 
        #define   INST8(id, nm, info, fmt, e1, e2, e3, e4, e5, e6, e7, e8                           ) 
        #define   INST9(id, nm, info, fmt, e1, e2, e3, e4, e5, e6, e7, e8, e9                       ) 
        #define  INST11(id, nm, info, fmt, e1, e2, e3, e4, e5, e6, e7, e8, e9, e10, e11             ) e10,
        #define  INST13(id, nm, info, fmt, e1, e2, e3, e4, e5, e6, e7, e8, e9, e10, e11, e12, e13   ) e10,
        #include "instrsarm64sve.h"
    };

    const static code_t insCodes11[] =
    {
        #define   INST1(id, nm, info, fmt, e1                                                       ) 
        #define   INST2(id, nm, info, fmt, e1, e2                                                   ) 
        #define   INST3(id, nm, info, fmt, e1, e2, e3                                               ) 
        #define   INST4(id, nm, info, fmt, e1, e2, e3, e4                                           ) 
        #define   INST5(id, nm, info, fmt, e1, e2, e3, e4, e5                                       ) 
        #define   INST6(id, nm, info, fmt, e1, e2, e3, e4, e5, e6                                   ) 
        #define   INST7(id, nm, info, fmt, e1, e2, e3, e4, e5, e6, e7                               ) 
        #define   INST8(id, nm, info, fmt, e1, e2, e3, e4, e5, e6, e7, e8                           ) 
        #define   INST9(id, nm, info, fmt, e1, e2, e3, e4, e5, e6, e7, e8, e9                       ) 
        #define  INST11(id, nm, info, fmt, e1, e2, e3, e4, e5, e6, e7, e8, e9, e10, e11             ) e11,
        #define  INST13(id, nm, info, fmt, e1, e2, e3, e4, e5, e6, e7, e8, e9, e10, e11, e12, e13   ) e11,
        #include "instrsarm64sve.h"
    };

    const static code_t insCodes12[] =
    {
        #define   INST1(id, nm, info, fmt, e1                                                       ) 
        #define   INST2(id, nm, info, fmt, e1, e2                                                   ) 
        #define   INST3(id, nm, info, fmt, e1, e2, e3                                               ) 
        #define   INST4(id, nm, info, fmt, e1, e2, e3, e4                                           ) 
        #define   INST5(id, nm, info, fmt, e1, e2, e3, e4, e5                                       ) 
        #define   INST6(id, nm, info, fmt, e1, e2, e3, e4, e5, e6                                   ) 
        #define   INST7(id, nm, info, fmt, e1, e2, e3, e4, e5, e6, e7                               ) 
        #define   INST8(id, nm, info, fmt, e1, e2, e3, e4, e5, e6, e7, e8                           ) 
        #define   INST9(id, nm, info, fmt, e1, e2, e3, e4, e5, e6, e7, e8, e9                       ) 
        #define  INST11(id, nm, info, fmt, e1, e2, e3, e4, e5, e6, e7, e8, e9, e10, e11             ) 
        #define  INST13(id, nm, info, fmt, e1, e2, e3, e4, e5, e6, e7, e8, e9, e10, e11, e12, e13   ) e12,
        #include "instrsarm64sve.h"
    };

    const static code_t insCodes13[] =
    {
        #define   INST1(id, nm, info, fmt, e1                                                       ) 
        #define   INST2(id, nm, info, fmt, e1, e2                                                   ) 
        #define   INST3(id, nm, info, fmt, e1, e2, e3                                               ) 
        #define   INST4(id, nm, info, fmt, e1, e2, e3, e4                                           ) 
        #define   INST5(id, nm, info, fmt, e1, e2, e3, e4, e5                                       ) 
        #define   INST6(id, nm, info, fmt, e1, e2, e3, e4, e5, e6                                   ) 
        #define   INST7(id, nm, info, fmt, e1, e2, e3, e4, e5, e6, e7                               ) 
        #define   INST8(id, nm, info, fmt, e1, e2, e3, e4, e5, e6, e7, e8                           ) 
        #define   INST9(id, nm, info, fmt, e1, e2, e3, e4, e5, e6, e7, e8, e9                       ) 
        #define  INST11(id, nm, info, fmt, e1, e2, e3, e4, e5, e6, e7, e8, e9, e10, e11             ) 
        #define  INST13(id, nm, info, fmt, e1, e2, e3, e4, e5, e6, e7, e8, e9, e10, e11, e12, e13   ) e13,
        #include "instrsarm64sve.h"
    };

    // clang-format on
    const static insFormat formatEncode13A[13] = {IF_SVE_AU_3A, IF_SVE_BT_1A, IF_SVE_BV_2A,   IF_SVE_BV_2A_J,
                                                  IF_SVE_BW_2A, IF_SVE_CB_2A, IF_SVE_CP_3A,   IF_SVE_CQ_3A,
                                                  IF_SVE_CW_4A, IF_SVE_CZ_4A, IF_SVE_CZ_4A_K, IF_SVE_CZ_4A_L,
                                                  IF_SVE_EB_1A};
    const static insFormat formatEncode11A[11] = {IF_SVE_JD_4B,   IF_SVE_JD_4C,   IF_SVE_JI_3A_A, IF_SVE_JJ_4A,
                                                  IF_SVE_JJ_4A_B, IF_SVE_JJ_4A_C, IF_SVE_JJ_4A_D, IF_SVE_JJ_4B,
                                                  IF_SVE_JJ_4B_E, IF_SVE_JN_3B,   IF_SVE_JN_3C};
    const static insFormat formatEncode9A[9]   = {IF_SVE_HW_4A,   IF_SVE_HW_4A_A, IF_SVE_HW_4A_B,
                                                  IF_SVE_HW_4A_C, IF_SVE_HW_4B,   IF_SVE_HW_4B_D,
                                                  IF_SVE_HX_3A_E, IF_SVE_IJ_3A_F, IF_SVE_IK_4A_G};
    const static insFormat formatEncode9B[9]   = {IF_SVE_HW_4A,   IF_SVE_HW_4A_A, IF_SVE_HW_4A_B,
                                                  IF_SVE_HW_4A_C, IF_SVE_HW_4B,   IF_SVE_HW_4B_D,
                                                  IF_SVE_HX_3A_E, IF_SVE_IJ_3A_G, IF_SVE_IK_4A_I};
    const static insFormat formatEncode9C[9]   = {IF_SVE_HW_4A,   IF_SVE_HW_4A_A, IF_SVE_HW_4A_B,
                                                  IF_SVE_HW_4A_C, IF_SVE_HW_4B,   IF_SVE_HW_4B_D,
                                                  IF_SVE_HX_3A_E, IF_SVE_IH_3A_F, IF_SVE_II_4A_H};
    const static insFormat formatEncode9D[9]   = {IF_SVE_IH_3A,   IF_SVE_IH_3A_A, IF_SVE_II_4A,
                                                  IF_SVE_II_4A_B, IF_SVE_IU_4A,   IF_SVE_IU_4A_C,
                                                  IF_SVE_IU_4B,   IF_SVE_IU_4B_D, IF_SVE_IV_3A};
    const static insFormat formatEncode9E[9]   = {IF_SVE_JD_4A,   IF_SVE_JI_3A_A, IF_SVE_JJ_4A,
                                                  IF_SVE_JJ_4A_B, IF_SVE_JJ_4A_C, IF_SVE_JJ_4A_D,
                                                  IF_SVE_JJ_4B,   IF_SVE_JJ_4B_E, IF_SVE_JN_3A};
    const static insFormat formatEncode9F[9]   = {IF_SVE_JD_4C,   IF_SVE_JD_4C_A, IF_SVE_JJ_4A,
                                                  IF_SVE_JJ_4A_B, IF_SVE_JJ_4B,   IF_SVE_JJ_4B_C,
                                                  IF_SVE_JL_3A,   IF_SVE_JN_3C,   IF_SVE_JN_3C_D};
    const static insFormat formatEncode8A[8]   = {IF_SVE_CE_2A, IF_SVE_CE_2B, IF_SVE_CE_2C, IF_SVE_CE_2D,
                                                  IF_SVE_CF_2A, IF_SVE_CF_2B, IF_SVE_CF_2C, IF_SVE_CF_2D};
    const static insFormat formatEncode8B[8]   = {IF_SVE_HW_4A, IF_SVE_HW_4A_A, IF_SVE_HW_4A_B, IF_SVE_HW_4A_C,
                                                  IF_SVE_HW_4B, IF_SVE_HW_4B_D, IF_SVE_HX_3A_E, IF_SVE_IG_4A_F};
    const static insFormat formatEncode8C[8]   = {IF_SVE_HW_4A, IF_SVE_HW_4A_A, IF_SVE_HW_4A_B, IF_SVE_HW_4A_C,
                                                  IF_SVE_HW_4B, IF_SVE_HW_4B_D, IF_SVE_HX_3A_E, IF_SVE_IG_4A_G};
    const static insFormat formatEncode7A[7]   = {IF_SVE_IJ_3A, IF_SVE_IK_4A,   IF_SVE_IU_4A, IF_SVE_IU_4A_A,
                                                  IF_SVE_IU_4B, IF_SVE_IU_4B_B, IF_SVE_IV_3A};
    const static insFormat formatEncode6A[6]   = {IF_SVE_AA_3A, IF_SVE_AT_3A, IF_SVE_EE_1A,
                                                  IF_SVE_FD_3A, IF_SVE_FD_3B, IF_SVE_FD_3C};
    const static insFormat formatEncode6B[6]   = {IF_SVE_GY_3A, IF_SVE_GY_3B,   IF_SVE_GY_3B_D,
                                                  IF_SVE_HA_3A, IF_SVE_HA_3A_E, IF_SVE_HA_3A_F};
    const static insFormat formatEncode6C[6]   = {IF_SVE_HW_4A,   IF_SVE_HW_4A_A, IF_SVE_HW_4B,
                                                  IF_SVE_HX_3A_B, IF_SVE_IJ_3A_D, IF_SVE_IK_4A_F};
    const static insFormat formatEncode6D[6]   = {IF_SVE_HW_4A,   IF_SVE_HW_4A_A, IF_SVE_HW_4B,
                                                  IF_SVE_HX_3A_B, IF_SVE_IJ_3A_E, IF_SVE_IK_4A_H};
    const static insFormat formatEncode6E[6]   = {IF_SVE_HY_3A,   IF_SVE_HY_3A_A, IF_SVE_HY_3B,
                                                  IF_SVE_HZ_2A_B, IF_SVE_IA_2A,   IF_SVE_IB_3A};
    const static insFormat formatEncode6F[6]   = {IF_SVE_IG_4A, IF_SVE_IU_4A,   IF_SVE_IU_4A_A,
                                                  IF_SVE_IU_4B, IF_SVE_IU_4B_B, IF_SVE_IV_3A};
    const static insFormat formatEncode6G[6]   = {IF_SVE_JD_4A,   IF_SVE_JI_3A_A, IF_SVE_JK_4A,
                                                  IF_SVE_JK_4A_B, IF_SVE_JK_4B,   IF_SVE_JN_3A};
    const static insFormat formatEncode5A[5]   = {IF_SVE_AM_2A, IF_SVE_AA_3A, IF_SVE_AO_3A, IF_SVE_BF_2A, IF_SVE_BG_3A};
    const static insFormat formatEncode5B[5]   = {IF_SVE_GX_3A, IF_SVE_GX_3B, IF_SVE_AT_3A, IF_SVE_HL_3A, IF_SVE_HM_2A};
    const static insFormat formatEncode5C[5]   = {IF_SVE_EF_3A, IF_SVE_EG_3A, IF_SVE_EH_3A, IF_SVE_EY_3A, IF_SVE_EY_3B};
    const static insFormat formatEncode5D[5]   = {IF_SVE_HW_4A, IF_SVE_HW_4A_A, IF_SVE_HW_4B, IF_SVE_HX_3A_B,
                                                  IF_SVE_IG_4A_D};
    const static insFormat formatEncode5E[5]   = {IF_SVE_HW_4A, IF_SVE_HW_4A_A, IF_SVE_HW_4B, IF_SVE_HX_3A_B,
                                                  IF_SVE_IG_4A_E};
    const static insFormat formatEncode4A[4]   = {IF_SVE_AA_3A, IF_SVE_AU_3A, IF_SVE_BS_1A, IF_SVE_CZ_4A};
    const static insFormat formatEncode4B[4]   = {IF_SVE_BU_2A, IF_SVE_BV_2B, IF_SVE_EA_1A, IF_SVE_EB_1B};
    const static insFormat formatEncode4E[4]   = {IF_SVE_AT_3A, IF_SVE_FI_3A, IF_SVE_FI_3B, IF_SVE_FI_3C};
    const static insFormat formatEncode4F[4]   = {IF_SVE_EM_3A, IF_SVE_FK_3A, IF_SVE_FK_3B, IF_SVE_FK_3C};
    const static insFormat formatEncode4G[4]   = {IF_SVE_AR_4A, IF_SVE_FF_3A, IF_SVE_FF_3B, IF_SVE_FF_3C};
    const static insFormat formatEncode4H[4]   = {IF_SVE_GM_3A, IF_SVE_GN_3A, IF_SVE_GZ_3A, IF_SVE_HB_3A};
    const static insFormat formatEncode4I[4]   = {IF_SVE_AX_1A, IF_SVE_AY_2A, IF_SVE_AZ_2A, IF_SVE_BA_3A};
    const static insFormat formatEncode4J[4]   = {IF_SVE_BV_2A, IF_SVE_BV_2A_A, IF_SVE_CP_3A, IF_SVE_CQ_3A};
    const static insFormat formatEncode4K[4]   = {IF_SVE_IF_4A, IF_SVE_IF_4A_A, IF_SVE_IM_3A, IF_SVE_IN_4A};
    const static insFormat formatEncode4L[4]   = {IF_SVE_IZ_4A, IF_SVE_IZ_4A_A, IF_SVE_JB_4A, IF_SVE_JM_3A};
    const static insFormat formatEncode3A[3]   = {IF_SVE_AA_3A, IF_SVE_AT_3A, IF_SVE_EC_1A};
    const static insFormat formatEncode3B[3]   = {IF_SVE_BH_3A, IF_SVE_BH_3B, IF_SVE_BH_3B_A};
    const static insFormat formatEncode3C[3]   = {IF_SVE_BW_2A, IF_SVE_CB_2A, IF_SVE_EB_1A};
    const static insFormat formatEncode3D[3]   = {IF_SVE_AT_3A, IF_SVE_BR_3B, IF_SVE_CI_3A};
    const static insFormat formatEncode3E[3]   = {IF_SVE_AT_3A, IF_SVE_EC_1A, IF_SVE_AA_3A};
    const static insFormat formatEncode3F[3]   = {IF_SVE_GU_3A, IF_SVE_GU_3B, IF_SVE_HU_4A};
    const static insFormat formatEncode3G[3]   = {IF_SVE_GH_3A, IF_SVE_GH_3B, IF_SVE_GH_3B_B};
    const static insFormat formatEncode3H[3]   = {IF_SVE_AT_3A, IF_SVE_HL_3A, IF_SVE_HM_2A};
    const static insFormat formatEncode3I[3]   = {IF_SVE_CM_3A, IF_SVE_CN_3A, IF_SVE_CO_3A};
    const static insFormat formatEncode3J[3]   = {IF_SVE_CX_4A, IF_SVE_CX_4A_A, IF_SVE_CY_3A};
    const static insFormat formatEncode3K[3]   = {IF_SVE_CX_4A, IF_SVE_CX_4A_A, IF_SVE_CY_3B};
    const static insFormat formatEncode3L[3]   = {IF_SVE_DT_3A, IF_SVE_DX_3A, IF_SVE_DY_3A};
    const static insFormat formatEncode3M[3]   = {IF_SVE_EJ_3A, IF_SVE_FA_3A, IF_SVE_FA_3B};
    const static insFormat formatEncode3N[3]   = {IF_SVE_EK_3A, IF_SVE_FB_3A, IF_SVE_FB_3B};
    const static insFormat formatEncode3O[3]   = {IF_SVE_EK_3A, IF_SVE_FC_3A, IF_SVE_FC_3B};
    const static insFormat formatEncode3P[3]   = {IF_SVE_EL_3A, IF_SVE_FG_3A, IF_SVE_FG_3B};
    const static insFormat formatEncode3Q[3]   = {IF_SVE_EL_3A, IF_SVE_FJ_3A, IF_SVE_FJ_3B};
    const static insFormat formatEncode3R[3]   = {IF_SVE_FE_3A, IF_SVE_FE_3B, IF_SVE_FL_3A};
    const static insFormat formatEncode3S[3]   = {IF_SVE_FH_3A, IF_SVE_FH_3B, IF_SVE_FL_3A};
    const static insFormat formatEncode3T[3]   = {IF_SVE_GX_3C, IF_SVE_HK_3B, IF_SVE_HL_3B};
    const static insFormat formatEncode3U[3]   = {IF_SVE_IM_3A, IF_SVE_IN_4A, IF_SVE_IX_4A};
    const static insFormat formatEncode3V[3]   = {IF_SVE_JA_4A, IF_SVE_JB_4A, IF_SVE_JM_3A};
    const static insFormat formatEncode2AA[2]  = {IF_SVE_ID_2A, IF_SVE_IE_2A};
    const static insFormat formatEncode2AB[2]  = {IF_SVE_JG_2A, IF_SVE_JH_2A};
    const static insFormat formatEncode2AC[2]  = {IF_SVE_AA_3A, IF_SVE_ED_1A};
    const static insFormat formatEncode2AD[2]  = {IF_SVE_AB_3B, IF_SVE_AT_3B};
    const static insFormat formatEncode2AE[2]  = {IF_SVE_CG_2A, IF_SVE_CJ_2A};
    const static insFormat formatEncode2AF[2]  = {IF_SVE_AA_3A, IF_SVE_AT_3A};
    const static insFormat formatEncode2AG[2]  = {IF_SVE_BS_1A, IF_SVE_CZ_4A};
    const static insFormat formatEncode2AH[2]  = {IF_SVE_BQ_2A, IF_SVE_BQ_2B};
    const static insFormat formatEncode2AI[2]  = {IF_SVE_AM_2A, IF_SVE_AA_3A};
    const static insFormat formatEncode2AJ[2]  = {IF_SVE_HI_3A, IF_SVE_HT_4A};
    const static insFormat formatEncode2AK[2]  = {IF_SVE_BZ_3A, IF_SVE_BZ_3A_A};
    const static insFormat formatEncode2AL[2]  = {IF_SVE_GG_3A, IF_SVE_GG_3B};
    const static insFormat formatEncode2AM[2]  = {IF_SVE_HL_3A, IF_SVE_HM_2A};
    const static insFormat formatEncode2AN[2]  = {IF_SVE_EI_3A, IF_SVE_EZ_3A};
    const static insFormat formatEncode2AO[2]  = {IF_SVE_GT_4A, IF_SVE_GV_3A};
    const static insFormat formatEncode2AP[2]  = {IF_SVE_GY_3B, IF_SVE_HA_3A};
    const static insFormat formatEncode2AQ[2]  = {IF_SVE_GO_3A, IF_SVE_HC_3A};
    const static insFormat formatEncode2AR[2]  = {IF_SVE_AP_3A, IF_SVE_CZ_4A};
    const static insFormat formatEncode2AT[2]  = {IF_SVE_AA_3A, IF_SVE_EC_1A};
    const static insFormat formatEncode2AU[2]  = {IF_SVE_AH_3A, IF_SVE_BI_2A};
    const static insFormat formatEncode2AV[2]  = {IF_SVE_BM_1A, IF_SVE_BN_1A};
    const static insFormat formatEncode2AW[2]  = {IF_SVE_BO_1A, IF_SVE_BP_1A};
    const static insFormat formatEncode2AX[2]  = {IF_SVE_CC_2A, IF_SVE_CD_2A};
    const static insFormat formatEncode2AY[2]  = {IF_SVE_CR_3A, IF_SVE_CS_3A};
    const static insFormat formatEncode2AZ[2]  = {IF_SVE_CV_3A, IF_SVE_CV_3B};
    const static insFormat formatEncode2BA[2]  = {IF_SVE_CW_4A, IF_SVE_CZ_4A};
    const static insFormat formatEncode2BB[2]  = {IF_SVE_CZ_4A, IF_SVE_CZ_4A_A};
    const static insFormat formatEncode2BC[2]  = {IF_SVE_DE_1A, IF_SVE_DZ_1A};
    const static insFormat formatEncode2BD[2]  = {IF_SVE_DG_2A, IF_SVE_DH_1A};
    const static insFormat formatEncode2BE[2]  = {IF_SVE_DK_3A, IF_SVE_DL_2A};
    const static insFormat formatEncode2BF[2]  = {IF_SVE_DM_2A, IF_SVE_DN_2A};
    const static insFormat formatEncode2BG[2]  = {IF_SVE_DO_2A, IF_SVE_DP_2A};
    const static insFormat formatEncode2BH[2]  = {IF_SVE_DW_2A, IF_SVE_DW_2B};
    const static insFormat formatEncode2BI[2]  = {IF_SVE_FL_3A, IF_SVE_FN_3B};
    const static insFormat formatEncode2BJ[2]  = {IF_SVE_GQ_3A, IF_SVE_HG_2A};
    const static insFormat formatEncode2BK[2]  = {IF_SVE_GU_3C, IF_SVE_HU_4B};
    const static insFormat formatEncode2BL[2]  = {IF_SVE_GZ_3A, IF_SVE_HB_3A};
    const static insFormat formatEncode2BM[2]  = {IF_SVE_HK_3B, IF_SVE_HL_3B};
    const static insFormat formatEncode2BN[2]  = {IF_SVE_IF_4A, IF_SVE_IF_4A_A};
    const static insFormat formatEncode2BO[2]  = {IF_SVE_IO_3A, IF_SVE_IP_4A};
    const static insFormat formatEncode2BP[2]  = {IF_SVE_IQ_3A, IF_SVE_IR_4A};
    const static insFormat formatEncode2BQ[2]  = {IF_SVE_IS_3A, IF_SVE_IT_4A};
    const static insFormat formatEncode2BR[2]  = {IF_SVE_JC_4A, IF_SVE_JO_3A};
    const static insFormat formatEncode2BS[2]  = {IF_SVE_JE_3A, IF_SVE_JF_4A};

    code_t    code           = BAD_CODE;
    insFormat insFmt         = emitInsFormat(ins);
    bool      encoding_found = false;
    int       index          = -1;

    switch (insFmt)
    {
        case IF_SVE_13A:
            for (index = 0; index < 13; index++)
            {
                if (fmt == formatEncode13A[index])
                {
                    encoding_found = true;
                    break;
                }
            }
            break;
        case IF_SVE_11A:
            for (index = 0; index < 11; index++)
            {
                if (fmt == formatEncode11A[index])
                {
                    encoding_found = true;
                    break;
                }
            }
            break;
        case IF_SVE_9A:
            for (index = 0; index < 9; index++)
            {
                if (fmt == formatEncode9A[index])
                {
                    encoding_found = true;
                    break;
                }
            }
            break;
        case IF_SVE_9B:
            for (index = 0; index < 9; index++)
            {
                if (fmt == formatEncode9B[index])
                {
                    encoding_found = true;
                    break;
                }
            }
            break;
        case IF_SVE_9C:
            for (index = 0; index < 9; index++)
            {
                if (fmt == formatEncode9C[index])
                {
                    encoding_found = true;
                    break;
                }
            }
            break;
        case IF_SVE_9D:
            for (index = 0; index < 9; index++)
            {
                if (fmt == formatEncode9D[index])
                {
                    encoding_found = true;
                    break;
                }
            }
            break;
        case IF_SVE_9E:
            for (index = 0; index < 9; index++)
            {
                if (fmt == formatEncode9E[index])
                {
                    encoding_found = true;
                    break;
                }
            }
            break;
        case IF_SVE_9F:
            for (index = 0; index < 9; index++)
            {
                if (fmt == formatEncode9F[index])
                {
                    encoding_found = true;
                    break;
                }
            }
            break;
        case IF_SVE_8A:
            for (index = 0; index < 8; index++)
            {
                if (fmt == formatEncode8A[index])
                {
                    encoding_found = true;
                    break;
                }
            }
            break;
        case IF_SVE_8B:
            for (index = 0; index < 8; index++)
            {
                if (fmt == formatEncode8B[index])
                {
                    encoding_found = true;
                    break;
                }
            }
            break;
        case IF_SVE_8C:
            for (index = 0; index < 8; index++)
            {
                if (fmt == formatEncode8C[index])
                {
                    encoding_found = true;
                    break;
                }
            }
            break;
        case IF_SVE_7A:
            for (index = 0; index < 7; index++)
            {
                if (fmt == formatEncode7A[index])
                {
                    encoding_found = true;
                    break;
                }
            }
            break;
        case IF_SVE_6A:
            for (index = 0; index < 6; index++)
            {
                if (fmt == formatEncode6A[index])
                {
                    encoding_found = true;
                    break;
                }
            }
            break;
        case IF_SVE_6B:
            for (index = 0; index < 6; index++)
            {
                if (fmt == formatEncode6B[index])
                {
                    encoding_found = true;
                    break;
                }
            }
            break;
        case IF_SVE_6C:
            for (index = 0; index < 6; index++)
            {
                if (fmt == formatEncode6C[index])
                {
                    encoding_found = true;
                    break;
                }
            }
            break;
        case IF_SVE_6D:
            for (index = 0; index < 6; index++)
            {
                if (fmt == formatEncode6D[index])
                {
                    encoding_found = true;
                    break;
                }
            }
            break;
        case IF_SVE_6E:
            for (index = 0; index < 6; index++)
            {
                if (fmt == formatEncode6E[index])
                {
                    encoding_found = true;
                    break;
                }
            }
            break;
        case IF_SVE_6F:
            for (index = 0; index < 6; index++)
            {
                if (fmt == formatEncode6F[index])
                {
                    encoding_found = true;
                    break;
                }
            }
            break;
        case IF_SVE_6G:
            for (index = 0; index < 6; index++)
            {
                if (fmt == formatEncode6G[index])
                {
                    encoding_found = true;
                    break;
                }
            }
            break;
        case IF_SVE_5A:
            for (index = 0; index < 5; index++)
            {
                if (fmt == formatEncode5A[index])
                {
                    encoding_found = true;
                    break;
                }
            }
            break;
        case IF_SVE_5B:
            for (index = 0; index < 5; index++)
            {
                if (fmt == formatEncode5B[index])
                {
                    encoding_found = true;
                    break;
                }
            }
            break;
        case IF_SVE_5C:
            for (index = 0; index < 5; index++)
            {
                if (fmt == formatEncode5C[index])
                {
                    encoding_found = true;
                    break;
                }
            }
            break;
        case IF_SVE_5D:
            for (index = 0; index < 5; index++)
            {
                if (fmt == formatEncode5D[index])
                {
                    encoding_found = true;
                    break;
                }
            }
            break;
        case IF_SVE_5E:
            for (index = 0; index < 5; index++)
            {
                if (fmt == formatEncode5E[index])
                {
                    encoding_found = true;
                    break;
                }
            }
            break;
        case IF_SVE_4A:
            for (index = 0; index < 4; index++)
            {
                if (fmt == formatEncode4A[index])
                {
                    encoding_found = true;
                    break;
                }
            }
            break;
        case IF_SVE_4B:
            for (index = 0; index < 4; index++)
            {
                if (fmt == formatEncode4B[index])
                {
                    encoding_found = true;
                    break;
                }
            }
            break;
        case IF_SVE_4E:
            for (index = 0; index < 4; index++)
            {
                if (fmt == formatEncode4E[index])
                {
                    encoding_found = true;
                    break;
                }
            }
            break;
        case IF_SVE_4F:
            for (index = 0; index < 4; index++)
            {
                if (fmt == formatEncode4F[index])
                {
                    encoding_found = true;
                    break;
                }
            }
            break;
        case IF_SVE_4G:
            for (index = 0; index < 4; index++)
            {
                if (fmt == formatEncode4G[index])
                {
                    encoding_found = true;
                    break;
                }
            }
            break;
        case IF_SVE_4H:
            for (index = 0; index < 4; index++)
            {
                if (fmt == formatEncode4H[index])
                {
                    encoding_found = true;
                    break;
                }
            }
            break;
        case IF_SVE_4I:
            for (index = 0; index < 4; index++)
            {
                if (fmt == formatEncode4I[index])
                {
                    encoding_found = true;
                    break;
                }
            }
            break;
        case IF_SVE_4J:
            for (index = 0; index < 4; index++)
            {
                if (fmt == formatEncode4J[index])
                {
                    encoding_found = true;
                    break;
                }
            }
            break;
        case IF_SVE_4K:
            for (index = 0; index < 4; index++)
            {
                if (fmt == formatEncode4K[index])
                {
                    encoding_found = true;
                    break;
                }
            }
            break;
        case IF_SVE_4L:
            for (index = 0; index < 4; index++)
            {
                if (fmt == formatEncode4L[index])
                {
                    encoding_found = true;
                    break;
                }
            }
            break;
        case IF_SVE_3A:
            for (index = 0; index < 3; index++)
            {
                if (fmt == formatEncode3A[index])
                {
                    encoding_found = true;
                    break;
                }
            }
            break;
        case IF_SVE_3B:
            for (index = 0; index < 3; index++)
            {
                if (fmt == formatEncode3B[index])
                {
                    encoding_found = true;
                    break;
                }
            }
            break;
        case IF_SVE_3C:
            for (index = 0; index < 3; index++)
            {
                if (fmt == formatEncode3C[index])
                {
                    encoding_found = true;
                    break;
                }
            }
            break;
        case IF_SVE_3D:
            for (index = 0; index < 3; index++)
            {
                if (fmt == formatEncode3D[index])
                {
                    encoding_found = true;
                    break;
                }
            }
            break;
        case IF_SVE_3E:
            for (index = 0; index < 3; index++)
            {
                if (fmt == formatEncode3E[index])
                {
                    encoding_found = true;
                    break;
                }
            }
            break;
        case IF_SVE_3F:
            for (index = 0; index < 3; index++)
            {
                if (fmt == formatEncode3F[index])
                {
                    encoding_found = true;
                    break;
                }
            }
            break;
        case IF_SVE_3G:
            for (index = 0; index < 3; index++)
            {
                if (fmt == formatEncode3G[index])
                {
                    encoding_found = true;
                    break;
                }
            }
            break;
        case IF_SVE_3H:
            for (index = 0; index < 3; index++)
            {
                if (fmt == formatEncode3H[index])
                {
                    encoding_found = true;
                    break;
                }
            }
            break;
        case IF_SVE_3I:
            for (index = 0; index < 3; index++)
            {
                if (fmt == formatEncode3I[index])
                {
                    encoding_found = true;
                    break;
                }
            }
            break;
        case IF_SVE_3J:
            for (index = 0; index < 3; index++)
            {
                if (fmt == formatEncode3J[index])
                {
                    encoding_found = true;
                    break;
                }
            }
            break;
        case IF_SVE_3K:
            for (index = 0; index < 3; index++)
            {
                if (fmt == formatEncode3K[index])
                {
                    encoding_found = true;
                    break;
                }
            }
            break;
        case IF_SVE_3L:
            for (index = 0; index < 3; index++)
            {
                if (fmt == formatEncode3L[index])
                {
                    encoding_found = true;
                    break;
                }
            }
            break;
        case IF_SVE_3M:
            for (index = 0; index < 3; index++)
            {
                if (fmt == formatEncode3M[index])
                {
                    encoding_found = true;
                    break;
                }
            }
            break;
        case IF_SVE_3N:
            for (index = 0; index < 3; index++)
            {
                if (fmt == formatEncode3N[index])
                {
                    encoding_found = true;
                    break;
                }
            }
            break;
        case IF_SVE_3O:
            for (index = 0; index < 3; index++)
            {
                if (fmt == formatEncode3O[index])
                {
                    encoding_found = true;
                    break;
                }
            }
            break;
        case IF_SVE_3P:
            for (index = 0; index < 3; index++)
            {
                if (fmt == formatEncode3P[index])
                {
                    encoding_found = true;
                    break;
                }
            }
            break;
        case IF_SVE_3Q:
            for (index = 0; index < 3; index++)
            {
                if (fmt == formatEncode3Q[index])
                {
                    encoding_found = true;
                    break;
                }
            }
            break;
        case IF_SVE_3R:
            for (index = 0; index < 3; index++)
            {
                if (fmt == formatEncode3R[index])
                {
                    encoding_found = true;
                    break;
                }
            }
            break;
        case IF_SVE_3S:
            for (index = 0; index < 3; index++)
            {
                if (fmt == formatEncode3S[index])
                {
                    encoding_found = true;
                    break;
                }
            }
            break;
        case IF_SVE_3T:
            for (index = 0; index < 3; index++)
            {
                if (fmt == formatEncode3T[index])
                {
                    encoding_found = true;
                    break;
                }
            }
            break;
        case IF_SVE_3U:
            for (index = 0; index < 3; index++)
            {
                if (fmt == formatEncode3U[index])
                {
                    encoding_found = true;
                    break;
                }
            }
            break;
        case IF_SVE_3V:
            for (index = 0; index < 3; index++)
            {
                if (fmt == formatEncode3V[index])
                {
                    encoding_found = true;
                    break;
                }
            }
            break;
        case IF_SVE_2AA:
            for (index = 0; index < 2; index++)
            {
                if (fmt == formatEncode2AA[index])
                {
                    encoding_found = true;
                    break;
                }
            }
            break;
        case IF_SVE_2AB:
            for (index = 0; index < 2; index++)
            {
                if (fmt == formatEncode2AB[index])
                {
                    encoding_found = true;
                    break;
                }
            }
            break;
        case IF_SVE_2AC:
            for (index = 0; index < 2; index++)
            {
                if (fmt == formatEncode2AC[index])
                {
                    encoding_found = true;
                    break;
                }
            }
            break;
        case IF_SVE_2AD:
            for (index = 0; index < 2; index++)
            {
                if (fmt == formatEncode2AD[index])
                {
                    encoding_found = true;
                    break;
                }
            }
            break;
        case IF_SVE_2AE:
            for (index = 0; index < 2; index++)
            {
                if (fmt == formatEncode2AE[index])
                {
                    encoding_found = true;
                    break;
                }
            }
            break;
        case IF_SVE_2AF:
            for (index = 0; index < 2; index++)
            {
                if (fmt == formatEncode2AF[index])
                {
                    encoding_found = true;
                    break;
                }
            }
            break;
        case IF_SVE_2AG:
            for (index = 0; index < 2; index++)
            {
                if (fmt == formatEncode2AG[index])
                {
                    encoding_found = true;
                    break;
                }
            }
            break;
        case IF_SVE_2AH:
            for (index = 0; index < 2; index++)
            {
                if (fmt == formatEncode2AH[index])
                {
                    encoding_found = true;
                    break;
                }
            }
            break;
        case IF_SVE_2AI:
            for (index = 0; index < 2; index++)
            {
                if (fmt == formatEncode2AI[index])
                {
                    encoding_found = true;
                    break;
                }
            }
            break;
        case IF_SVE_2AJ:
            for (index = 0; index < 2; index++)
            {
                if (fmt == formatEncode2AJ[index])
                {
                    encoding_found = true;
                    break;
                }
            }
            break;
        case IF_SVE_2AK:
            for (index = 0; index < 2; index++)
            {
                if (fmt == formatEncode2AK[index])
                {
                    encoding_found = true;
                    break;
                }
            }
            break;
        case IF_SVE_2AL:
            for (index = 0; index < 2; index++)
            {
                if (fmt == formatEncode2AL[index])
                {
                    encoding_found = true;
                    break;
                }
            }
            break;
        case IF_SVE_2AM:
            for (index = 0; index < 2; index++)
            {
                if (fmt == formatEncode2AM[index])
                {
                    encoding_found = true;
                    break;
                }
            }
            break;
        case IF_SVE_2AN:
            for (index = 0; index < 2; index++)
            {
                if (fmt == formatEncode2AN[index])
                {
                    encoding_found = true;
                    break;
                }
            }
            break;
        case IF_SVE_2AO:
            for (index = 0; index < 2; index++)
            {
                if (fmt == formatEncode2AO[index])
                {
                    encoding_found = true;
                    break;
                }
            }
            break;
        case IF_SVE_2AP:
            for (index = 0; index < 2; index++)
            {
                if (fmt == formatEncode2AP[index])
                {
                    encoding_found = true;
                    break;
                }
            }
            break;
        case IF_SVE_2AQ:
            for (index = 0; index < 2; index++)
            {
                if (fmt == formatEncode2AQ[index])
                {
                    encoding_found = true;
                    break;
                }
            }
            break;
        case IF_SVE_2AR:
            for (index = 0; index < 2; index++)
            {
                if (fmt == formatEncode2AR[index])
                {
                    encoding_found = true;
                    break;
                }
            }
            break;
        case IF_SVE_2AT:
            for (index = 0; index < 2; index++)
            {
                if (fmt == formatEncode2AT[index])
                {
                    encoding_found = true;
                    break;
                }
            }
            break;
        case IF_SVE_2AU:
            for (index = 0; index < 2; index++)
            {
                if (fmt == formatEncode2AU[index])
                {
                    encoding_found = true;
                    break;
                }
            }
            break;
        case IF_SVE_2AV:
            for (index = 0; index < 2; index++)
            {
                if (fmt == formatEncode2AV[index])
                {
                    encoding_found = true;
                    break;
                }
            }
            break;
        case IF_SVE_2AW:
            for (index = 0; index < 2; index++)
            {
                if (fmt == formatEncode2AW[index])
                {
                    encoding_found = true;
                    break;
                }
            }
            break;
        case IF_SVE_2AX:
            for (index = 0; index < 2; index++)
            {
                if (fmt == formatEncode2AX[index])
                {
                    encoding_found = true;
                    break;
                }
            }
            break;
        case IF_SVE_2AY:
            for (index = 0; index < 2; index++)
            {
                if (fmt == formatEncode2AY[index])
                {
                    encoding_found = true;
                    break;
                }
            }
            break;
        case IF_SVE_2AZ:
            for (index = 0; index < 2; index++)
            {
                if (fmt == formatEncode2AZ[index])
                {
                    encoding_found = true;
                    break;
                }
            }
            break;
        case IF_SVE_2BA:
            for (index = 0; index < 2; index++)
            {
                if (fmt == formatEncode2BA[index])
                {
                    encoding_found = true;
                    break;
                }
            }
            break;
        case IF_SVE_2BB:
            for (index = 0; index < 2; index++)
            {
                if (fmt == formatEncode2BB[index])
                {
                    encoding_found = true;
                    break;
                }
            }
            break;
        case IF_SVE_2BC:
            for (index = 0; index < 2; index++)
            {
                if (fmt == formatEncode2BC[index])
                {
                    encoding_found = true;
                    break;
                }
            }
            break;
        case IF_SVE_2BD:
            for (index = 0; index < 2; index++)
            {
                if (fmt == formatEncode2BD[index])
                {
                    encoding_found = true;
                    break;
                }
            }
            break;
        case IF_SVE_2BE:
            for (index = 0; index < 2; index++)
            {
                if (fmt == formatEncode2BE[index])
                {
                    encoding_found = true;
                    break;
                }
            }
            break;
        case IF_SVE_2BF:
            for (index = 0; index < 2; index++)
            {
                if (fmt == formatEncode2BF[index])
                {
                    encoding_found = true;
                    break;
                }
            }
            break;
        case IF_SVE_2BG:
            for (index = 0; index < 2; index++)
            {
                if (fmt == formatEncode2BG[index])
                {
                    encoding_found = true;
                    break;
                }
            }
            break;
        case IF_SVE_2BH:
            for (index = 0; index < 2; index++)
            {
                if (fmt == formatEncode2BH[index])
                {
                    encoding_found = true;
                    break;
                }
            }
            break;
        case IF_SVE_2BI:
            for (index = 0; index < 2; index++)
            {
                if (fmt == formatEncode2BI[index])
                {
                    encoding_found = true;
                    break;
                }
            }
            break;
        case IF_SVE_2BJ:
            for (index = 0; index < 2; index++)
            {
                if (fmt == formatEncode2BJ[index])
                {
                    encoding_found = true;
                    break;
                }
            }
            break;
        case IF_SVE_2BK:
            for (index = 0; index < 2; index++)
            {
                if (fmt == formatEncode2BK[index])
                {
                    encoding_found = true;
                    break;
                }
            }
            break;
        case IF_SVE_2BL:
            for (index = 0; index < 2; index++)
            {
                if (fmt == formatEncode2BL[index])
                {
                    encoding_found = true;
                    break;
                }
            }
            break;
        case IF_SVE_2BM:
            for (index = 0; index < 2; index++)
            {
                if (fmt == formatEncode2BM[index])
                {
                    encoding_found = true;
                    break;
                }
            }
            break;
        case IF_SVE_2BN:
            for (index = 0; index < 2; index++)
            {
                if (fmt == formatEncode2BN[index])
                {
                    encoding_found = true;
                    break;
                }
            }
            break;
        case IF_SVE_2BO:
            for (index = 0; index < 2; index++)
            {
                if (fmt == formatEncode2BO[index])
                {
                    encoding_found = true;
                    break;
                }
            }
            break;
        case IF_SVE_2BP:
            for (index = 0; index < 2; index++)
            {
                if (fmt == formatEncode2BP[index])
                {
                    encoding_found = true;
                    break;
                }
            }
            break;
        case IF_SVE_2BQ:
            for (index = 0; index < 2; index++)
            {
                if (fmt == formatEncode2BQ[index])
                {
                    encoding_found = true;
                    break;
                }
            }
            break;
        case IF_SVE_2BR:
            for (index = 0; index < 2; index++)
            {
                if (fmt == formatEncode2BR[index])
                {
                    encoding_found = true;
                    break;
                }
            }
            break;
        case IF_SVE_2BS:
            for (index = 0; index < 2; index++)
            {
                if (fmt == formatEncode2BS[index])
                {
                    encoding_found = true;
                    break;
                }
            }
            break;
        default:
            if (fmt == insFmt)
            {
                encoding_found = true;
                index          = 0;
            }
            else
            {
                encoding_found = false;
            }
            break;
    }

    assert(encoding_found);
    const unsigned sve_ins_offset = ((unsigned)ins - INS_sve_invalid);

    switch (index)
    {
        case 0:
            assert(sve_ins_offset < ArrLen(insCodes1));
            code = insCodes1[sve_ins_offset];
            break;
        case 1:
            assert(sve_ins_offset < ArrLen(insCodes2));
            code = insCodes2[sve_ins_offset];
            break;
        case 2:
            assert(sve_ins_offset < ArrLen(insCodes3));
            code = insCodes3[sve_ins_offset];
            break;
        case 3:
            assert(sve_ins_offset < ArrLen(insCodes4));
            code = insCodes4[sve_ins_offset];
            break;
        case 4:
            assert(sve_ins_offset < ArrLen(insCodes5));
            code = insCodes5[sve_ins_offset];
            break;
        case 5:
            assert(sve_ins_offset < ArrLen(insCodes6));
            code = insCodes6[sve_ins_offset];
            break;
        case 6:
            assert(sve_ins_offset < ArrLen(insCodes7));
            code = insCodes7[sve_ins_offset];
            break;
        case 7:
            assert(sve_ins_offset < ArrLen(insCodes8));
            code = insCodes8[sve_ins_offset];
            break;
        case 8:
            assert(sve_ins_offset < ArrLen(insCodes9));
            code = insCodes9[sve_ins_offset];
            break;
        case 9:
            assert(sve_ins_offset < ArrLen(insCodes10));
            code = insCodes10[sve_ins_offset];
            break;
        case 10:
            assert(sve_ins_offset < ArrLen(insCodes11));
            code = insCodes11[sve_ins_offset];
            break;
        case 11:
            assert(sve_ins_offset < ArrLen(insCodes12));
            code = insCodes12[sve_ins_offset];
            break;
        case 12:
            assert(sve_ins_offset < ArrLen(insCodes13));
            code = insCodes13[sve_ins_offset];
            break;
    }

    assert((code != BAD_CODE));

    return code;
}

/*****************************************************************************
 *
 *  Add a SVE instruction with a single immediate value.
 */

void emitter::emitInsSve_I(instruction ins, emitAttr attr, ssize_t imm)
{
    insFormat fmt;

    /* Figure out the encoding format of the instruction */
    if (ins == INS_sve_setffr)
    {
        fmt  = IF_SVE_DQ_0A;
        attr = EA_PTRSIZE;
        imm  = 0;
    }
    else
    {
        unreached();
    }

    instrDesc* id = emitNewInstrSC(attr, imm);

    id->idIns(ins);
    id->idInsFmt(fmt);

    dispIns(id);
    appendToCurIG(id);
}

/*****************************************************************************
 *
 *  Add a SVE instruction referencing a single register.
 */

void emitter::emitInsSve_R(instruction ins, emitAttr attr, regNumber reg, insOpts opt /* = INS_OPTS_NONE */)
{
    insFormat fmt;

    /* Figure out the encoding format of the instruction */
    switch (ins)
    {
        case INS_sve_aesmc:
        case INS_sve_aesimc:
            opt = INS_OPTS_SCALABLE_B;
            assert(isVectorRegister(reg)); // ddddd
            assert(isScalableVectorSize(attr));
            fmt = IF_SVE_GL_1A;
            break;

        case INS_sve_rdffr:
            opt = INS_OPTS_SCALABLE_B;
            assert(isPredicateRegister(reg)); // DDDD
            fmt = IF_SVE_DH_1A;
            break;

        case INS_sve_pfalse:
            opt = INS_OPTS_SCALABLE_B;
            assert(isPredicateRegister(reg)); // DDDD
            fmt = IF_SVE_DJ_1A;
            break;

        case INS_sve_wrffr:
            opt = INS_OPTS_SCALABLE_B;
            assert(isPredicateRegister(reg)); // NNNN
            fmt = IF_SVE_DR_1A;
            break;

        case INS_sve_ptrue:
            assert(insOptsScalableStandard(opt));
            assert(isHighPredicateRegister(reg));                  // DDD
            assert(isValidVectorElemsize(optGetSveElemsize(opt))); // xx
            fmt = IF_SVE_DZ_1A;
            break;

        case INS_sve_fmov:
            assert(insOptsScalableStandard(opt));
            assert(isVectorRegister(reg));                         // ddddd
            assert(isValidVectorElemsize(optGetSveElemsize(opt))); // xx
            fmt = IF_SVE_EB_1B;

            // FMOV is a pseudo-instruction for DUP, which is aliased by MOV;
            // MOV is the preferred disassembly
            ins = INS_sve_mov;
            break;

        default:
            unreached();
            break;
    }

    instrDesc* id = emitNewInstrSmall(attr);

    id->idIns(ins);
    id->idInsFmt(fmt);
    id->idInsOpt(opt);
    id->idReg1(reg);

    dispIns(id);
    appendToCurIG(id);
}

/*****************************************************************************
 *
 *  Add a SVE instruction referencing a register and a constant.
 */

void emitter::emitInsSve_R_I(instruction     ins,
                             emitAttr        attr,
                             regNumber       reg,
                             ssize_t         imm,
                             insOpts         opt, /* = INS_OPTS_NONE */
                             insScalableOpts sopt /* = INS_SCALABLE_OPTS_NONE */)
{
    emitAttr  size      = EA_SIZE(attr);
    bool      canEncode = false;
    bool      signedImm = false;
    bool      hasShift  = false;
    insFormat fmt;

    /* Figure out the encoding format of the instruction */
    switch (ins)
    {
        bitMaskImm bmi;

        case INS_sve_rdvl:
            assert(insOptsNone(opt));
            assert(size == EA_8BYTE);
            assert(isGeneralRegister(reg)); // ddddd
            assert(isValidSimm<6>(imm));    // iiiiii
            fmt       = IF_SVE_BC_1A;
            canEncode = true;
            break;

        case INS_sve_smax:
        case INS_sve_smin:
            signedImm = true;

            FALLTHROUGH;
        case INS_sve_umax:
        case INS_sve_umin:
            assert(insOptsScalableStandard(opt));
            assert(isVectorRegister(reg));                         // ddddd
            assert(isValidVectorElemsize(optGetSveElemsize(opt))); // xx

            if (signedImm)
            {
                assert(isValidSimm<8>(imm)); // iiiiiiii
            }
            else
            {
                assert(isValidUimm<8>(imm)); // iiiiiiii
            }

            fmt       = IF_SVE_ED_1A;
            canEncode = true;
            break;

        case INS_sve_mul:
            assert(insOptsScalableStandard(opt));
            assert(isVectorRegister(reg));                         // ddddd
            assert(isValidSimm<8>(imm));                           // iiiiiiii
            assert(isValidVectorElemsize(optGetSveElemsize(opt))); // xx
            fmt       = IF_SVE_EE_1A;
            canEncode = true;
            break;

        case INS_sve_mov:
            assert(insOptsScalableStandard(opt));
            assert(isVectorRegister(reg)); // ddddd

            if (sopt == INS_SCALABLE_OPTS_IMM_BITMASK)
            {
                bmi.immNRS = 0;
                canEncode  = canEncodeBitMaskImm(imm, optGetSveElemsize(opt), &bmi);

                if (!useMovDisasmForBitMask(imm))
                {
                    ins = INS_sve_dupm;
                }

                imm = bmi.immNRS; // iiiiiiiiiiiii
                assert(isValidImmNRS(imm, optGetSveElemsize(opt)));
                fmt = IF_SVE_BT_1A;
            }
            else
            {
                assert(insScalableOptsNone(sopt));
                assert(isValidVectorElemsize(optGetSveElemsize(opt))); // xx

                if (!isValidSimm<8>(imm))
                {
                    // Size specifier must be able to fit a left-shifted immediate
                    assert((isValidSimm_MultipleOf<8, 256>(imm))); // iiiiiiii
                    assert(insOptsScalableAtLeastHalf(opt));
                    hasShift = true;
                    imm >>= 8;
                }

                fmt       = IF_SVE_EB_1A;
                canEncode = true;
            }
            break;

        case INS_sve_dup:
            assert(insOptsScalableStandard(opt));
            assert(isVectorRegister(reg));                         // ddddd
            assert(isValidVectorElemsize(optGetSveElemsize(opt))); // xx

            if (!isValidSimm<8>(imm))
            {
                // Size specifier must be able to fit a left-shifted immediate
                assert((isValidSimm_MultipleOf<8, 256>(imm))); // iiiiiiii
                assert(insOptsScalableAtLeastHalf(opt));
                hasShift = true;
                imm >>= 8;
            }

            fmt       = IF_SVE_EB_1A;
            canEncode = true;

            // MOV is an alias for DUP, and is always the preferred disassembly.
            ins = INS_sve_mov;
            break;

        case INS_sve_add:
        case INS_sve_sub:
        case INS_sve_sqadd:
        case INS_sve_sqsub:
        case INS_sve_uqadd:
        case INS_sve_uqsub:
        case INS_sve_subr:
            assert(insOptsScalableStandard(opt));
            assert(isVectorRegister(reg));                         // ddddd
            assert(isValidVectorElemsize(optGetSveElemsize(opt))); // xx
            if (!isValidUimm<8>(imm))
            {
                // Size specifier must be able to fit left-shifted immediate
                assert((isValidUimm_MultipleOf<8, 256>(imm))); // iiiiiiii
                assert(insOptsScalableAtLeastHalf(opt));
                hasShift = true;
                imm >>= 8;
            }

            fmt       = IF_SVE_EC_1A;
            canEncode = true;
            break;

        case INS_sve_and:
        case INS_sve_orr:
        case INS_sve_eor:
            assert(insOptsScalableStandard(opt));
            assert(isVectorRegister(reg)); // ddddd

            bmi.immNRS = 0;
            canEncode  = canEncodeBitMaskImm(imm, optGetSveElemsize(opt), &bmi);
            imm        = bmi.immNRS; // iiiiiiiiiiiii
            assert(isValidImmNRS(imm, optGetSveElemsize(opt)));
            fmt = IF_SVE_BS_1A;
            break;

        case INS_sve_bic:
            assert(insOptsScalableStandard(opt));
            assert(isVectorRegister(reg)); // ddddd

            // AND is an alias for BIC, and is always the preferred disassembly.
            ins = INS_sve_and;
            imm = -imm - 1;

            bmi.immNRS = 0;
            canEncode  = canEncodeBitMaskImm(imm, optGetSveElemsize(opt), &bmi);
            imm        = bmi.immNRS; // iiiiiiiiiiiii
            assert(isValidImmNRS(imm, optGetSveElemsize(opt)));
            fmt = IF_SVE_BS_1A;
            break;

        case INS_sve_eon:
            assert(insOptsScalableStandard(opt));
            assert(isVectorRegister(reg)); // ddddd

            // EOR is an alias for EON, and is always the preferred disassembly.
            ins = INS_sve_eor;
            imm = -imm - 1;

            bmi.immNRS = 0;
            canEncode  = canEncodeBitMaskImm(imm, optGetSveElemsize(opt), &bmi);
            imm        = bmi.immNRS; // iiiiiiiiiiiii
            assert(isValidImmNRS(imm, optGetSveElemsize(opt)));
            fmt = IF_SVE_BS_1A;
            break;

        case INS_sve_orn:
            assert(insOptsScalableStandard(opt));
            assert(isVectorRegister(reg)); // ddddd

            // ORR is an alias for ORN, and is always the preferred disassembly.
            ins = INS_sve_orr;
            imm = -imm - 1;

            bmi.immNRS = 0;
            canEncode  = canEncodeBitMaskImm(imm, optGetSveElemsize(opt), &bmi);
            imm        = bmi.immNRS; // iiiiiiiiiiiii
            assert(isValidImmNRS(imm, optGetSveElemsize(opt)));
            fmt = IF_SVE_BS_1A;
            break;

        case INS_sve_dupm:
            assert(insOptsScalableStandard(opt));
            assert(isVectorRegister(reg)); // ddddd

            bmi.immNRS = 0;
            canEncode  = canEncodeBitMaskImm(imm, optGetSveElemsize(opt), &bmi);
            fmt        = IF_SVE_BT_1A;

            if (useMovDisasmForBitMask(imm))
            {
                ins = INS_sve_mov;
            }

            imm = bmi.immNRS; // iiiiiiiiiiiii
            assert(isValidImmNRS(imm, optGetSveElemsize(opt)));
            break;

        default:
            unreached();
            break;
    }

    assert(canEncode);

    // For encodings with shifted immediates, we need a way to determine if the immediate has been shifted or not.
    // We could just leave the immediate in its unshifted form, and call emitNewInstrSC,
    // but that would allocate unnecessarily large descriptors. Therefore:
    // - For encodings without any shifting, just call emitNewInstrSC.
    // - For unshifted immediates, call emitNewInstrSC.
    //   If it allocates a small descriptor, idHasShift() will always return false.
    //   Else, idHasShift still returns false, as we set the dedicated bit in large descriptors to false.
    // - For immediates that need a shift, call emitNewInstrCns so a normal or large descriptor is used.
    //   idHasShift will always check the dedicated bit, as it is always available. We set this bit to true below.
    instrDesc* id = !hasShift ? emitNewInstrSC(attr, imm) : emitNewInstrCns(attr, imm);

    id->idIns(ins);
    id->idInsFmt(fmt);
    id->idInsOpt(opt);

    id->idReg1(reg);

    id->idHasShift(hasShift);

    dispIns(id);
    appendToCurIG(id);
}

/*****************************************************************************
 *
 *  Add a SVE instruction referencing a register and a floating point constant.
 */

void emitter::emitInsSve_R_F(
    instruction ins, emitAttr attr, regNumber reg, double immDbl, insOpts opt /* = INS_OPTS_NONE */)
{
    ssize_t   imm       = 0;
    bool      canEncode = false;
    insFormat fmt;

    /* Figure out the encoding format of the instruction */
    switch (ins)
    {
        floatImm8 fpi;

        case INS_sve_fmov:
        case INS_sve_fdup:
            assert(insOptsScalableAtLeastHalf(opt));
            assert(isVectorRegister(reg));                         // ddddd
            assert(isValidVectorElemsize(optGetSveElemsize(opt))); // xx

            fpi.immFPIVal = 0;
            canEncode     = canEncodeFloatImm8(immDbl, &fpi);
            imm           = fpi.immFPIVal;
            fmt           = IF_SVE_EA_1A;

            // FMOV is an alias for FDUP, and is always the preferred disassembly.
            ins = INS_sve_fmov;
            break;

        default:
            unreached();
            break;
    }

    assert(canEncode);

    instrDesc* id = emitNewInstrSC(attr, imm);

    id->idIns(ins);
    id->idInsFmt(fmt);
    id->idInsOpt(opt);

    id->idReg1(reg);

    dispIns(id);
    appendToCurIG(id);
}

/*****************************************************************************
 *
 *  Add a SVE instruction referencing two registers
 */

void emitter::emitInsSve_R_R(instruction     ins,
                             emitAttr        attr,
                             regNumber       reg1,
                             regNumber       reg2,
                             insOpts         opt /* = INS_OPTS_NONE */,
                             insScalableOpts sopt /* = INS_SCALABLE_OPTS_NONE */)
{
    emitAttr  size = EA_SIZE(attr);
    insFormat fmt  = IF_NONE;

    /* Figure out the encoding format of the instruction */
    switch (ins)
    {
        case INS_sve_pmov:
            if (opt != INS_OPTS_SCALABLE_B)
            {
                assert(insOptsScalableStandard(opt));
                return emitInsSve_R_R_I(INS_sve_pmov, attr, reg1, reg2, 0, opt, sopt);
            }
            if (isPredicateRegister(reg1))
            {
                assert(isVectorRegister(reg2));
                fmt = IF_SVE_CE_2A;
            }
            else
            {
                assert(isVectorRegister(reg1));
                assert(isPredicateRegister(reg2));
                fmt = IF_SVE_CF_2A;
            }
            break;

        case INS_sve_movs:
        {
            assert(opt == INS_OPTS_SCALABLE_B);
            assert(isPredicateRegister(reg1)); // dddd
            assert(isPredicateRegister(reg2)); // nnnn
            fmt = IF_SVE_CZ_4A_A;
            break;
        }

        case INS_sve_mov:
        {
            if (isGeneralRegisterOrSP(reg2))
            {
                assert(insScalableOptsNone(sopt));
                assert(insOptsScalableStandard(opt));
                assert(isVectorRegister(reg1));
#ifdef DEBUG
                if (opt == INS_OPTS_SCALABLE_D)
                {
                    assert(size == EA_8BYTE);
                }
                else
                {
                    assert(size == EA_4BYTE);
                }
#endif // DEBUG
                reg2 = encodingSPtoZR(reg2);
                fmt  = IF_SVE_CB_2A;
            }
            else
            {
                assert(opt == INS_OPTS_SCALABLE_B);
                assert(isPredicateRegister(reg1)); // dddd
                assert(isPredicateRegister(reg2)); // nnnn
                fmt = IF_SVE_CZ_4A_L;
            }
            break;
        }

        case INS_sve_insr:
            assert(insOptsScalableStandard(opt));
            assert(isVectorRegister(reg1)); // ddddd
            if (isVectorRegister(reg2))
            {
                fmt = IF_SVE_CC_2A;
            }
            else if (isGeneralRegisterOrZR(reg2))
            {
                fmt = IF_SVE_CD_2A;
            }
            else
            {
                unreached();
            }
            break;

        case INS_sve_pfirst:
            assert(opt == INS_OPTS_SCALABLE_B);
            assert(isPredicateRegister(reg1)); // DDDD
            assert(isPredicateRegister(reg2)); // gggg
            fmt = IF_SVE_DD_2A;
            break;

        case INS_sve_pnext:
            assert(insOptsScalableStandard(opt));
            assert(isPredicateRegister(reg1));                     // DDDD
            assert(isPredicateRegister(reg2));                     // VVVV
            assert(isValidVectorElemsize(optGetSveElemsize(opt))); // xx
            fmt = IF_SVE_DF_2A;
            break;

        case INS_sve_punpkhi:
        case INS_sve_punpklo:
            assert(isPredicateRegister(reg1)); // DDDD
            assert(isPredicateRegister(reg2)); // NNNN
            fmt = IF_SVE_CK_2A;
            break;

        case INS_sve_rdffr:
        case INS_sve_rdffrs:
            assert(opt == INS_OPTS_SCALABLE_B);
            assert(isPredicateRegister(reg1)); // DDDD
            assert(isPredicateRegister(reg2)); // gggg
            fmt = IF_SVE_DG_2A;
            break;

        case INS_sve_rev:
            assert(insScalableOptsNone(sopt));
            if (isVectorRegister(reg1))
            {
                assert(insOptsScalableStandard(opt));
                assert(isVectorRegister(reg2));
                assert(isScalableVectorSize(size));
                fmt = IF_SVE_CG_2A;
            }
            else
            {
                assert(insOptsScalableStandard(opt));
                assert(isPredicateRegister(reg1)); // DDDD
                assert(isPredicateRegister(reg2)); // NNNN
                fmt = IF_SVE_CJ_2A;
            }
            break;

        case INS_sve_ptest:
            assert(opt == INS_OPTS_SCALABLE_B);
            assert(isPredicateRegister(reg1)); // gggg
            assert(isPredicateRegister(reg2)); // NNNN
            fmt = IF_SVE_DI_2A;
            break;

        case INS_sve_cntp:
            assert(isScalableVectorSize(size));
            assert(insOptsScalableStandard(opt));
            assert(insScalableOptsWithVectorLength(sopt));         // l
            assert(isGeneralRegister(reg1));                       // ddddd
            assert(isPredicateRegister(reg2));                     // NNNN
            assert(isValidVectorElemsize(optGetSveElemsize(opt))); // xx
            fmt = IF_SVE_DL_2A;
            break;

        case INS_sve_incp:
        case INS_sve_decp:
            assert(isPredicateRegister(reg2)); // MMMM

            if (isGeneralRegister(reg1)) // ddddd
            {
                assert(insOptsScalableStandard(opt)); // xx
                assert(size == EA_8BYTE);
                fmt = IF_SVE_DM_2A;
            }
            else
            {
                assert(insOptsScalableAtLeastHalf(opt)); // xx
                assert(isVectorRegister(reg1));          // ddddd
                assert(isScalableVectorSize(size));
                fmt = IF_SVE_DN_2A;
            }
            break;

        case INS_sve_sqincp:
        case INS_sve_uqincp:
        case INS_sve_sqdecp:
        case INS_sve_uqdecp:
            assert(isPredicateRegister(reg2)); // MMMM

            if (isGeneralRegister(reg1)) // ddddd
            {
                assert(insOptsScalableStandard(opt)); // xx
                assert(isValidGeneralDatasize(size));
                fmt = IF_SVE_DO_2A;
            }
            else
            {
                assert(insOptsScalableAtLeastHalf(opt)); // xx
                assert(isVectorRegister(reg1));          // ddddd
                assert(isScalableVectorSize(size));
                fmt = IF_SVE_DP_2A;
            }
            break;

        case INS_sve_ctermeq:
        case INS_sve_ctermne:
            assert(insOptsNone(opt));
            assert(isGeneralRegister(reg1));      // nnnnn
            assert(isGeneralRegister(reg2));      // mmmmm
            assert(isValidGeneralDatasize(size)); // x
            fmt = IF_SVE_DS_2A;
            break;

        case INS_sve_sqcvtn:
        case INS_sve_uqcvtn:
        case INS_sve_sqcvtun:
            assert(insOptsNone(opt));
            assert(isVectorRegister(reg1)); // ddddd
            assert(isVectorRegister(reg2)); // nnnn
            assert(isEvenRegister(reg2));
            fmt = IF_SVE_FZ_2A;
            break;

        case INS_sve_fcvtn:
        case INS_sve_bfcvtn:
        case INS_sve_fcvtnt:
        case INS_sve_fcvtnb:
            unreached(); // TODO-SVE: Not yet supported.
            assert(insOptsNone(opt));
            assert(isVectorRegister(reg1)); // ddddd
            assert(isVectorRegister(reg2)); // nnnn
            assert(isEvenRegister(reg2));
            fmt = IF_SVE_HG_2A;
            break;

        case INS_sve_sqxtnb:
        case INS_sve_sqxtnt:
        case INS_sve_uqxtnb:
        case INS_sve_uqxtnt:
        case INS_sve_sqxtunb:
        case INS_sve_sqxtunt:
            assert(insOptsScalableStandard(opt));
            assert(isVectorRegister(reg1));
            assert(isVectorRegister(reg2));
            assert(optGetSveElemsize(opt) != EA_8BYTE);
            assert(isValidVectorElemsize(optGetSveElemsize(opt)));
            assert(isScalableVectorSize(size));
            fmt = IF_SVE_GD_2A;
            break;

        case INS_sve_aese:
        case INS_sve_aesd:
        case INS_sve_sm4e:
            assert(insOptsScalableStandard(opt));
            assert(isVectorRegister(reg1));
            assert(isVectorRegister(reg2));
#ifdef DEBUG
            if (opt == INS_OPTS_SCALABLE_S)
            {
                assert(ins == INS_sve_sm4e);
            }
            else
            {
                assert(opt == INS_OPTS_SCALABLE_B);
            }
#endif // DEBUG
            fmt = IF_SVE_GK_2A;
            break;

        case INS_sve_frecpe:
        case INS_sve_frsqrte:
            assert(insScalableOptsNone(sopt));
            assert(insOptsScalableAtLeastHalf(opt));
            assert(isVectorRegister(reg1));
            assert(isVectorRegister(reg2));
            assert(isScalableVectorSize(size));
            fmt = IF_SVE_HF_2A;
            break;

        case INS_sve_sunpkhi:
        case INS_sve_sunpklo:
        case INS_sve_uunpkhi:
        case INS_sve_uunpklo:
            assert(insScalableOptsNone(sopt));
            assert(insOptsScalableWide(opt));
            assert(isVectorRegister(reg1));
            assert(isVectorRegister(reg2));
            assert(isScalableVectorSize(size));
            fmt = IF_SVE_CH_2A;
            break;

        case INS_sve_fexpa:
            assert(insScalableOptsNone(sopt));
            assert(insOptsScalableAtLeastHalf(opt));
            assert(isVectorRegister(reg1));
            assert(isVectorRegister(reg2));
            assert(isScalableVectorSize(size));
            fmt = IF_SVE_BJ_2A;
            break;

        case INS_sve_dup:
            assert(insScalableOptsNone(sopt));
            assert(insOptsScalableStandard(opt));
            assert(isVectorRegister(reg1));
            assert(isGeneralRegisterOrSP(reg2));
#ifdef DEBUG
            if (opt == INS_OPTS_SCALABLE_D)
            {
                assert(size == EA_8BYTE);
            }
            else
            {
                assert(size == EA_4BYTE);
            }
#endif // DEBUG
            reg2 = encodingSPtoZR(reg2);
            fmt  = IF_SVE_CB_2A;

            // DUP is an alias for MOV;
            // MOV is the preferred disassembly
            ins = INS_sve_mov;
            break;

        case INS_sve_bf1cvt:
        case INS_sve_bf1cvtlt:
        case INS_sve_bf2cvt:
        case INS_sve_bf2cvtlt:
        case INS_sve_f1cvt:
        case INS_sve_f1cvtlt:
        case INS_sve_f2cvt:
        case INS_sve_f2cvtlt:
            assert(insScalableOptsNone(sopt));
            assert(opt == INS_OPTS_SCALABLE_H);
            assert(isVectorRegister(reg1));
            assert(isVectorRegister(reg2));
            assert(isScalableVectorSize(size));
            fmt = IF_SVE_HH_2A;
            unreached(); // not supported yet
            break;

        case INS_sve_movprfx:
            assert(insScalableOptsNone(sopt));
            assert(insOptsNone(opt));
            assert(isVectorRegister(reg1));
            assert(isVectorRegister(reg2));
            assert(isScalableVectorSize(size));
            fmt = IF_SVE_BI_2A;
            break;

        case INS_sve_fmov:
            assert(insOptsScalableAtLeastHalf(opt));
            assert(isVectorRegister(reg1));                        // ddddd
            assert(isPredicateRegister(reg2));                     // gggg
            assert(isValidVectorElemsize(optGetSveElemsize(opt))); // xx
            fmt = IF_SVE_BV_2B;

            // CPY is an alias for FMOV, and MOV is an alias for CPY.
            // Thus, MOV is the preferred disassembly.
            ins = INS_sve_mov;
            break;

        default:
            unreached();
            break;
    }

    assert(fmt != IF_NONE);

    instrDesc* id;

    if (insScalableOptsWithVectorLength(sopt))
    {
        id = emitNewInstr(attr);
        id->idVectorLength4x(sopt == INS_SCALABLE_OPTS_VL_4X);
    }
    else
    {
        id = emitNewInstrSmall(attr);
    }

    id->idIns(ins);
    id->idInsFmt(fmt);
    id->idInsOpt(opt);

    id->idReg1(reg1);
    id->idReg2(reg2);

    dispIns(id);
    appendToCurIG(id);
}

/*****************************************************************************
 *
 *  Add a SVE instruction referencing a register and two constants.
 */

void emitter::emitInsSve_R_I_I(
    instruction ins, emitAttr attr, regNumber reg, ssize_t imm1, ssize_t imm2, insOpts opt /* = INS_OPTS_NONE */)
{
    insFormat fmt;
    ssize_t   immOut;

    if (ins == INS_sve_index)
    {
        assert(insOptsScalableStandard(opt));
        assert(isVectorRegister(reg));                         // ddddd
        assert(isValidSimm<5>(imm1));                          // iiiii
        assert(isValidSimm<5>(imm2));                          // iiiii
        assert(isValidVectorElemsize(optGetSveElemsize(opt))); // xx
        immOut = insSveEncodeTwoSimm5(imm1, imm2);
        fmt    = IF_SVE_AX_1A;
    }
    else
    {
        unreached();
    }

    instrDesc* id = emitNewInstrSC(attr, immOut);

    id->idIns(ins);
    id->idInsFmt(fmt);
    id->idInsOpt(opt);

    id->idReg1(reg);

    dispIns(id);
    appendToCurIG(id);
}

/*****************************************************************************
 *
 *  Add a SVE instruction referencing two registers and a constant.
 */

void emitter::emitInsSve_R_R_I(instruction     ins,
                               emitAttr        attr,
                               regNumber       reg1,
                               regNumber       reg2,
                               ssize_t         imm,
                               insOpts         opt /* = INS_OPTS_NONE */,
                               insScalableOpts sopt /* = INS_SCALABLE_OPTS_NONE */)
{
    emitAttr  size     = EA_SIZE(attr);
    bool      hasShift = false;
    insFormat fmt;

    /* Figure out the encoding format of the instruction */
    switch (ins)
    {
        bool isRightShift;

        case INS_sve_asr:
        case INS_sve_lsl:
        case INS_sve_lsr:
        case INS_sve_srshr:
        case INS_sve_sqshl:
        case INS_sve_urshr:
        case INS_sve_sqshlu:
        case INS_sve_uqshl:
        case INS_sve_asrd:
            isRightShift = emitInsIsVectorRightShift(ins);
            assert(isValidVectorShiftAmount(imm, optGetSveElemsize(opt), isRightShift));
            assert(insOptsScalableStandard(opt));
            assert(isScalableVectorSize(size));
            assert(insScalableOptsNone(sopt));
            if (isVectorRegister(reg2))
            {
                assert((ins == INS_sve_asr) || (ins == INS_sve_lsl) || (ins == INS_sve_lsr));
                assert(isVectorRegister(reg1));
                fmt = IF_SVE_BF_2A;
            }
            else
            {
                assert(isVectorRegister(reg1));       // ddddd
                assert(isLowPredicateRegister(reg2)); // ggg
                fmt = IF_SVE_AM_2A;
            }
            break;

        case INS_sve_xar:
            assert(insOptsScalableStandard(opt));
            assert(isVectorRegister(reg1));                        // ddddd
            assert(isVectorRegister(reg2));                        // mmmmm
            assert(isValidVectorElemsize(optGetSveElemsize(opt))); // xx xx

            switch (opt)
            {
                case INS_OPTS_SCALABLE_B:
                    assert(isValidUimmFrom1<3>(imm)); // iii
                    break;

                case INS_OPTS_SCALABLE_H:
                    assert(isValidUimmFrom1<4>(imm)); // xiii
                    break;

                case INS_OPTS_SCALABLE_S:
                    assert(isValidUimmFrom1<5>(imm)); // xxiii
                    break;

                case INS_OPTS_SCALABLE_D:
                    assert(isValidUimmFrom1<6>(imm)); // x xxiii
                    break;

                default:
                    unreached();
                    break;
            }

            fmt = IF_SVE_AW_2A;
            break;

        case INS_sve_index:
            assert(insOptsScalable(opt));
            assert(isVectorRegister(reg1));                        // ddddd
            assert(isValidSimm<5>(imm));                           // iiiii
            assert(isIntegerRegister(reg2));                       // mmmmm
            assert(isValidVectorElemsize(optGetSveElemsize(opt))); // xx

            if (sopt == INS_SCALABLE_OPTS_IMM_FIRST)
            {
                fmt = IF_SVE_AY_2A;
            }
            else
            {
                assert(insScalableOptsNone(sopt));
                fmt = IF_SVE_AZ_2A;
            }
            break;

        case INS_sve_addvl:
        case INS_sve_addpl:
            assert(insOptsNone(opt));
            assert(size == EA_8BYTE);
            assert(isGeneralRegisterOrSP(reg1)); // ddddd
            assert(isGeneralRegisterOrSP(reg2)); // nnnnn
            assert(isValidSimm<6>(imm));         // iiiiii
            reg1 = encodingSPtoZR(reg1);
            reg2 = encodingSPtoZR(reg2);
            fmt  = IF_SVE_BB_2A;
            break;

        case INS_sve_mov:
            if (isVectorRegister(reg2))
            {
                return emitInsSve_R_R_I(INS_sve_dup, attr, reg1, reg2, imm, opt, sopt);
            }
            FALLTHROUGH;
        case INS_sve_cpy:
            assert(insOptsScalableStandard(opt));
            assert(isVectorRegister(reg1));    // DDDDD
            assert(isPredicateRegister(reg2)); // GGGG

            if (!isValidSimm<8>(imm))
            {
                // Size specifier must be able to fit a left-shifted immediate
                assert((isValidSimm_MultipleOf<8, 256>(imm))); // iiiiiiii
                assert(insOptsScalableAtLeastHalf(opt));
                hasShift = true;
                imm >>= 8;
            }

            if (sopt == INS_SCALABLE_OPTS_PREDICATE_MERGE)
            {
                fmt = IF_SVE_BV_2A_J;
            }
            else
            {
                assert(insScalableOptsNone(sopt));
                fmt = IF_SVE_BV_2A;
            }

            // MOV is an alias for CPY, and is always the preferred disassembly.
            ins = INS_sve_mov;
            break;

        case INS_sve_dup:
            assert(insOptsScalable(opt));
            assert(isVectorRegister(reg1)); // DDDDD
            assert(isVectorRegister(reg2)); // GGGG
            assert(isValidBroadcastImm(imm, optGetSveElemsize(opt)));
            fmt = IF_SVE_BW_2A;
            ins = INS_sve_mov; // Set preferred alias for disassembly
            break;

        case INS_sve_pmov:
            if (isPredicateRegister(reg1))
            {
                assert(isVectorRegister(reg2));
                switch (opt)
                {
                    case INS_OPTS_SCALABLE_D:
                        assert(isValidUimm<3>(imm));
                        fmt = IF_SVE_CE_2B;
                        break;
                    case INS_OPTS_SCALABLE_S:
                        assert(isValidUimm<2>(imm));
                        fmt = IF_SVE_CE_2D;
                        break;
                    case INS_OPTS_SCALABLE_H:
                        assert(isValidUimm<1>(imm));
                        fmt = IF_SVE_CE_2C;
                        break;
                    default:
                        unreached();
                }
            }
            else
            {
                assert(isVectorRegister(reg1));
                assert(isPredicateRegister(reg2));
                switch (opt)
                {
                    case INS_OPTS_SCALABLE_D:
                        assert(isValidUimm<3>(imm));
                        fmt = IF_SVE_CF_2B;
                        break;
                    case INS_OPTS_SCALABLE_S:
                        assert(isValidUimm<2>(imm));
                        fmt = IF_SVE_CF_2D;
                        break;
                    case INS_OPTS_SCALABLE_H:
                        assert(isValidUimm<1>(imm));
                        fmt = IF_SVE_CF_2C;
                        break;
                    default:
                        unreached();
                }
            }
            break;

        case INS_sve_sqrshrn:
        case INS_sve_sqrshrun:
        case INS_sve_uqrshrn:
            isRightShift = emitInsIsVectorRightShift(ins);
            assert(isVectorRegister(reg1));
            assert(isVectorRegister(reg2));
            assert(isEvenRegister(reg2));
            assert(opt == INS_OPTS_SCALABLE_H);
            assert(isRightShift); // These are always right-shift.
            assert(isValidVectorShiftAmount(imm, EA_4BYTE, isRightShift));
            fmt = IF_SVE_GA_2A;
            break;

        case INS_sve_pext:
            assert(insOptsScalableStandard(opt));
            assert(isPredicateRegister(reg1));                     // DDDD
            assert(isHighPredicateRegister(reg2));                 // NNN
            assert(isValidVectorElemsize(optGetSveElemsize(opt))); // xx

            if (sopt == INS_SCALABLE_OPTS_WITH_PREDICATE_PAIR)
            {
                assert(isValidUimm<1>(imm)); // i
                fmt = IF_SVE_DW_2B;
            }
            else
            {
                assert(insScalableOptsNone(sopt));
                assert(isValidUimm<2>(imm)); // ii
                fmt = IF_SVE_DW_2A;
            }
            break;

        case INS_sve_sshllb:
        case INS_sve_sshllt:
        case INS_sve_ushllb:
        case INS_sve_ushllt:
            assert(insOptsScalableWide(opt));
            assert(isVectorRegister(reg1));                        // ddddd
            assert(isVectorRegister(reg2));                        // nnnnn
            assert(isValidVectorElemsize(optGetSveElemsize(opt))); // x xx

            switch (opt)
            {
                case INS_OPTS_SCALABLE_B:
                    assert(isValidUimm<3>(imm)); // iii
                    break;

                case INS_OPTS_SCALABLE_H:
                    assert(isValidUimm<4>(imm)); // x iii
                    break;

                case INS_OPTS_SCALABLE_S:
                    assert(isValidUimm<5>(imm)); // xx iii
                    break;

                default:
                    unreached();
                    break;
            }

            fmt = IF_SVE_FR_2A;
            break;

        case INS_sve_sqshrunb:
        case INS_sve_sqshrunt:
        case INS_sve_sqrshrunb:
        case INS_sve_sqrshrunt:
        case INS_sve_shrnb:
        case INS_sve_shrnt:
        case INS_sve_rshrnb:
        case INS_sve_rshrnt:
        case INS_sve_sqshrnb:
        case INS_sve_sqshrnt:
        case INS_sve_sqrshrnb:
        case INS_sve_sqrshrnt:
        case INS_sve_uqshrnb:
        case INS_sve_uqshrnt:
        case INS_sve_uqrshrnb:
        case INS_sve_uqrshrnt:
            assert(insOptsScalableWide(opt));
            assert(isVectorRegister(reg1));                        // ddddd
            assert(isVectorRegister(reg2));                        // nnnnn
            assert(isValidVectorElemsize(optGetSveElemsize(opt))); // x xx

            switch (opt)
            {
                case INS_OPTS_SCALABLE_B:
                    assert(isValidUimmFrom1<3>(imm)); // iii
                    break;

                case INS_OPTS_SCALABLE_H:
                    assert(isValidUimmFrom1<4>(imm)); // x iii
                    break;

                case INS_OPTS_SCALABLE_S:
                    assert(isValidUimmFrom1<5>(imm)); // xx iii
                    break;

                default:
                    unreached();
                    break;
            }

            fmt = IF_SVE_GB_2A;
            break;

        case INS_sve_cadd:
        case INS_sve_sqcadd:
            assert(insOptsScalableStandard(opt));
            assert(isVectorRegister(reg1));                        // ddddd
            assert(isVectorRegister(reg2));                        // nnnnn
            assert(isValidVectorElemsize(optGetSveElemsize(opt))); // xx

            // Convert rot to bitwise representation: 0 if 90, 1 if 270
            imm = emitEncodeRotationImm90_or_270(imm); // r
            fmt = IF_SVE_FV_2A;
            break;

        case INS_sve_ftmad:
            assert(insOptsScalableAtLeastHalf(opt));
            assert(insScalableOptsNone(sopt));
            assert(isVectorRegister(reg1));
            assert(isVectorRegister(reg2));
            assert(isValidUimm<3>(imm));
            assert(isScalableVectorSize(size));
            fmt = IF_SVE_HN_2A;
            break;

        case INS_sve_ldr:
            assert(insOptsNone(opt));
            assert(isScalableVectorSize(size));
            assert(isGeneralRegister(reg2)); // nnnnn
            assert(isValidSimm<9>(imm));     // iii
                                             // iiiiii

            assert(insScalableOptsNone(sopt));
            if (isVectorRegister(reg1))
            {
                fmt = IF_SVE_IE_2A;
            }
            else
            {
                assert(isPredicateRegister(reg1));
                fmt = IF_SVE_ID_2A;
            }
            break;

        case INS_sve_str:
            assert(insOptsNone(opt));
            assert(isScalableVectorSize(size));
            assert(isGeneralRegister(reg2)); // nnnnn
            assert(isValidSimm<9>(imm));     // iii
                                             // iiiiii

            assert(insScalableOptsNone(sopt));
            if (isVectorRegister(reg1))
            {
                fmt = IF_SVE_JH_2A;
            }
            else
            {
                assert(isPredicateRegister(reg1));
                fmt = IF_SVE_JG_2A;
            }
            break;

        case INS_sve_sli:
        case INS_sve_sri:
            isRightShift = emitInsIsVectorRightShift(ins);
            assert(isValidVectorShiftAmount(imm, optGetSveElemsize(opt), isRightShift));
            assert(insOptsScalableStandard(opt));
            assert(insScalableOptsNone(sopt));
            assert(isVectorRegister(reg1));
            assert(isVectorRegister(reg2));
            assert(isScalableVectorSize(size));
            fmt = IF_SVE_FT_2A;
            break;

        case INS_sve_srsra:
        case INS_sve_ssra:
        case INS_sve_ursra:
        case INS_sve_usra:
            isRightShift = emitInsIsVectorRightShift(ins);
            assert(isValidVectorShiftAmount(imm, optGetSveElemsize(opt), isRightShift));
            assert(insOptsScalableStandard(opt));
            assert(insScalableOptsNone(sopt));
            assert(isVectorRegister(reg1));
            assert(isVectorRegister(reg2));
            assert(isScalableVectorSize(size));
            fmt = IF_SVE_FU_2A;
            break;

        case INS_sve_ext:
            assert(opt == INS_OPTS_SCALABLE_B);
            assert(isVectorRegister(reg1)); // ddddd
            assert(isVectorRegister(reg2)); // nnnnn
            assert(isValidUimm<8>(imm));    // iiiii iii

            if (sopt == INS_SCALABLE_OPTS_WITH_VECTOR_PAIR)
            {
                fmt = IF_SVE_BQ_2A;
                unreached(); // Not supported yet.
            }
            else
            {
                assert(insScalableOptsNone(sopt));
                fmt = IF_SVE_BQ_2B;
            }
            break;

        case INS_sve_dupq:
            assert(insOptsScalableStandard(opt));
            assert(insScalableOptsNone(sopt));
            assert(isVectorRegister(reg1));
            assert(isVectorRegister(reg2));
            assert(isScalableVectorSize(size));
#ifdef DEBUG
            switch (opt)
            {
                case INS_OPTS_SCALABLE_B:
                    assert(isValidUimm<4>(imm));
                    break;

                case INS_OPTS_SCALABLE_H:
                    assert(isValidUimm<3>(imm));
                    break;

                case INS_OPTS_SCALABLE_S:
                    assert(isValidUimm<2>(imm));
                    break;

                case INS_OPTS_SCALABLE_D:
                    assert(isValidUimm<1>(imm));
                    break;

                default:
                    break;
            }
#endif // DEBUG
            fmt = IF_SVE_BX_2A;
            break;

        case INS_sve_extq:
            assert(opt == INS_OPTS_SCALABLE_B);
            assert(insScalableOptsNone(sopt));
            assert(isVectorRegister(reg1));
            assert(isVectorRegister(reg2));
            assert(isScalableVectorSize(size));
            assert(isValidUimm<4>(imm));
            fmt = IF_SVE_BY_2A;
            break;

        default:
            unreached();
            break;
    }

    // For encodings with shifted immediates, we need a way to determine if the immediate has been shifted or not.
    // We could just leave the immediate in its unshifted form, and call emitNewInstrSC,
    // but that would allocate unnecessarily large descriptors. Therefore:
    // - For encodings without any shifting, just call emitNewInstrSC.
    // - For unshifted immediates, call emitNewInstrSC.
    //   If it allocates a small descriptor, idHasShift() will always return false.
    //   Else, idHasShift still returns false, as we set the dedicated bit in large descriptors to false.
    // - For immediates that need a shift, call emitNewInstrCns so a normal or large descriptor is used.
    //   idHasShift will always check the dedicated bit, as it is always available. We set this bit to true below.
    instrDesc* id = !hasShift ? emitNewInstrSC(attr, imm) : emitNewInstrCns(attr, imm);

    id->idIns(ins);
    id->idInsFmt(fmt);
    id->idInsOpt(opt);

    id->idReg1(reg1);
    id->idReg2(reg2);

    id->idHasShift(hasShift);

    dispIns(id);
    appendToCurIG(id);
}

/*****************************************************************************
 *
 *  Add a SVE instruction referencing two registers and a floating point constant.
 */

void emitter::emitInsSve_R_R_F(
    instruction ins, emitAttr attr, regNumber reg1, regNumber reg2, double immDbl, insOpts opt /* = INS_OPTS_NONE */)
{
    ssize_t   imm  = 0;
    emitAttr  size = EA_SIZE(attr);
    insFormat fmt  = IF_NONE;

    /* Figure out the encoding format of the instruction */
    switch (ins)
    {
        case INS_sve_fmul:
        case INS_sve_fmaxnm:
        case INS_sve_fadd:
        case INS_sve_fmax:
        case INS_sve_fminnm:
        case INS_sve_fsub:
        case INS_sve_fmin:
        case INS_sve_fsubr:
            assert(insOptsScalableAtLeastHalf(opt));
            assert(isVectorRegister(reg1));
            assert(isLowPredicateRegister(reg2));
            assert(isScalableVectorSize(size));
            imm = emitEncodeSmallFloatImm(immDbl, ins);
            fmt = IF_SVE_HM_2A;
            break;

        case INS_sve_fmov:
        case INS_sve_fcpy:
            assert(insOptsScalableAtLeastHalf(opt));
            assert(isVectorRegister(reg1));                        // ddddd
            assert(isPredicateRegister(reg2));                     // gggg
            assert(isValidVectorElemsize(optGetSveElemsize(opt))); // xx
            floatImm8 fpi;
            fpi.immFPIVal = 0;
            canEncodeFloatImm8(immDbl, &fpi);
            imm = fpi.immFPIVal;
            fmt = IF_SVE_BU_2A;

            // FMOV is an alias for FCPY, and is always the preferred disassembly.
            ins = INS_sve_fmov;
            break;

        default:
            unreached();
            break;

    } // end switch (ins)

    assert(fmt != IF_NONE);

    instrDesc* id = emitNewInstrSC(attr, imm);

    id->idIns(ins);
    id->idInsFmt(fmt);
    id->idInsOpt(opt);

    id->idReg1(reg1);
    id->idReg2(reg2);

    dispIns(id);
    appendToCurIG(id);
}

/*****************************************************************************
 *
 *  Add a SVE instruction referencing three registers.
 */

void emitter::emitInsSve_R_R_R(instruction     ins,
                               emitAttr        attr,
                               regNumber       reg1,
                               regNumber       reg2,
                               regNumber       reg3,
                               insOpts         opt /* = INS_OPTS_NONE */,
                               insScalableOpts sopt /* = INS_SCALABLE_OPTS_NONE */)
{
    emitAttr  size           = EA_SIZE(attr);
    bool      pmerge         = false;
    bool      vectorLength4x = false;
    insFormat fmt            = IF_NONE;

    /* Figure out the encoding format of the instruction */
    switch (ins)
    {
        case INS_sve_and:
        case INS_sve_bic:
        case INS_sve_eor:
        case INS_sve_orr:
            assert(insOptsScalableStandard(opt));
            assert(isVectorRegister(reg1)); // mmmmm
            assert(isVectorRegister(reg3)); // ddddd
            assert(insScalableOptsNone(sopt));

            if (isVectorRegister(reg2))
            {
                // The instruction only has a .D variant. However, this doesn't matter as
                // it operates on bits not lanes. Effectively this means all standard opt
                // sizes are supported.
                assert(insOptsScalableStandard(opt));
                fmt = IF_SVE_AU_3A;
            }
            else
            {
                assert(isLowPredicateRegister(reg2)); // ggg
                fmt = IF_SVE_AA_3A;
            }
            break;

        case INS_sve_add:
        case INS_sve_sub:
        case INS_sve_subr:
            assert(isVectorRegister(reg1));
            assert(isVectorRegister(reg3));
            assert(insOptsScalableStandard(opt));
            assert(insScalableOptsNone(sopt));
            if (isVectorRegister(reg2))
            {
                assert(ins != INS_sve_subr);
                fmt = IF_SVE_AT_3A;
            }
            else
            {
                assert(isLowPredicateRegister(reg2));
                fmt = IF_SVE_AA_3A;
            }
            break;

        case INS_sve_addpt:
        case INS_sve_subpt:
            unreached(); // TODO-SVE: Not yet supported.
            assert(opt == INS_OPTS_SCALABLE_D);
            assert(isVectorRegister(reg1)); // ddddd
            assert(isVectorRegister(reg3)); // mmmmm
            assert(insScalableOptsNone(sopt));

            if (isVectorRegister(reg2))
            {
                fmt = IF_SVE_AT_3B;
            }
            else
            {
                assert(isLowPredicateRegister(reg2)); // ggg
                fmt = IF_SVE_AB_3B;
            }
            break;

        case INS_sve_sdiv:
        case INS_sve_sdivr:
        case INS_sve_udiv:
        case INS_sve_udivr:
            assert(isVectorRegister(reg1));
            assert(isLowPredicateRegister(reg2));
            assert(isVectorRegister(reg3));
            assert(insOptsScalableWords(opt));
            assert(insScalableOptsNone(sopt));
            fmt = IF_SVE_AC_3A;
            break;

        case INS_sve_sabd:
        case INS_sve_smax:
        case INS_sve_smin:
        case INS_sve_uabd:
        case INS_sve_umax:
        case INS_sve_umin:
            assert(isVectorRegister(reg1));
            assert(isLowPredicateRegister(reg2));
            assert(isVectorRegister(reg3));
            assert(insOptsScalableStandard(opt));
            assert(insScalableOptsNone(sopt));
            fmt = IF_SVE_AA_3A;
            break;

        case INS_sve_mul:
        case INS_sve_smulh:
        case INS_sve_umulh:
            assert(isVectorRegister(reg1));
            assert(isVectorRegister(reg3));
            assert(insOptsScalableStandard(opt));
            assert(insScalableOptsNone(sopt));
            if (isVectorRegister(reg2))
            {
                fmt = IF_SVE_AT_3A;
            }
            else
            {
                assert(isLowPredicateRegister(reg2));
                fmt = IF_SVE_AA_3A;
            }
            break;

        case INS_sve_pmul:
            assert(opt == INS_OPTS_SCALABLE_B);
            assert(isVectorRegister(reg1)); // ddddd
            assert(isVectorRegister(reg2)); // nnnnn
            assert(isVectorRegister(reg3)); // mmmmm
            fmt = IF_SVE_BD_3B;
            break;

        case INS_sve_andv:
        case INS_sve_eorv:
        case INS_sve_orv:
            assert(isFloatReg(reg1));
            assert(isLowPredicateRegister(reg2));
            assert(isVectorRegister(reg3));
            assert(insOptsScalableStandard(opt));
            assert(insScalableOptsNone(sopt));
            fmt = IF_SVE_AF_3A;
            break;

        case INS_sve_andqv:
        case INS_sve_eorqv:
        case INS_sve_orqv:
            unreached(); // TODO-SVE: Not yet supported.
            assert(isVectorRegister(reg1));
            assert(isLowPredicateRegister(reg2));
            assert(isVectorRegister(reg3));
            assert(insOptsScalableStandard(opt));
            assert(insScalableOptsNone(sopt));
            fmt = IF_SVE_AG_3A;
            break;

        case INS_sve_movprfx:
            assert(isVectorRegister(reg1));
            assert(isLowPredicateRegister(reg2));
            assert(isVectorRegister(reg3));
            assert(insOptsScalableStandard(opt));
            if (sopt == INS_SCALABLE_OPTS_PREDICATE_MERGE)
            {
                pmerge = true;
            }
            fmt = IF_SVE_AH_3A;
            break;

        case INS_sve_saddv:
            assert(isFloatReg(reg1));
            assert(isLowPredicateRegister(reg2));
            assert(isVectorRegister(reg3));
            assert(insOptsScalableWide(opt));
            assert(insScalableOptsNone(sopt));
            fmt = IF_SVE_AI_3A;
            break;

        case INS_sve_uaddv:
            assert(isFloatReg(reg1));
            assert(isLowPredicateRegister(reg2));
            assert(isVectorRegister(reg3));
            assert(insOptsScalableStandard(opt));
            assert(insScalableOptsNone(sopt));
            fmt = IF_SVE_AI_3A;
            break;

        case INS_sve_addqv:
            unreached(); // TODO-SVE: Not yet supported.
            assert(isVectorRegister(reg1));
            assert(isLowPredicateRegister(reg2));
            assert(isVectorRegister(reg3));
            assert(insOptsScalableStandard(opt));
            assert(insScalableOptsNone(sopt));
            fmt = IF_SVE_AJ_3A;
            break;

        case INS_sve_smaxv:
        case INS_sve_sminv:
        case INS_sve_umaxv:
        case INS_sve_uminv:
            assert(isFloatReg(reg1));
            assert(isLowPredicateRegister(reg2));
            assert(isVectorRegister(reg3));
            assert(insOptsScalableStandard(opt));
            assert(insScalableOptsNone(sopt));
            fmt = IF_SVE_AK_3A;
            break;

        case INS_sve_smaxqv:
        case INS_sve_sminqv:
        case INS_sve_umaxqv:
        case INS_sve_uminqv:
            unreached(); // TODO-SVE: Not yet supported.
            assert(isVectorRegister(reg1));
            assert(isLowPredicateRegister(reg2));
            assert(isVectorRegister(reg3));
            assert(insOptsScalableStandard(opt));
            assert(insScalableOptsNone(sopt));
            fmt = IF_SVE_AL_3A;
            break;

        case INS_sve_asrr:
        case INS_sve_lslr:
        case INS_sve_lsrr:
            assert(isVectorRegister(reg1));
            assert(isLowPredicateRegister(reg2));
            assert(isVectorRegister(reg3));
            assert(insOptsScalableStandard(opt));
            assert(insScalableOptsNone(sopt));
            fmt = IF_SVE_AA_3A;
            break;

        case INS_sve_asr:
        case INS_sve_lsl:
        case INS_sve_lsr:
            assert(isVectorRegister(reg1));
            assert(isVectorRegister(reg3));
            if (sopt == INS_SCALABLE_OPTS_WIDE)
            {
                assert(isLowPredicateRegister(reg2));
                assert(insOptsScalableWide(opt));
                fmt = IF_SVE_AO_3A;
            }
            else if (isVectorRegister(reg2))
            {
                assert(insScalableOptsNone(sopt));
                assert(insOptsScalableWide(opt));
                fmt = IF_SVE_BG_3A;
            }
            else
            {
                assert(isLowPredicateRegister(reg2));
                assert(insScalableOptsNone(sopt));
                assert(insOptsScalableStandard(opt));
                fmt = IF_SVE_AA_3A;
            }
            break;

        case INS_sve_uzp1:
        case INS_sve_trn1:
        case INS_sve_zip1:
        case INS_sve_uzp2:
        case INS_sve_trn2:
        case INS_sve_zip2:
            assert(insOptsScalable(opt));
            assert(insScalableOptsNone(sopt));

            if (isVectorRegister(reg1))
            {
                assert(isVectorRegister(reg2)); // nnnnn
                assert(isVectorRegister(reg3)); // mmmmm

                if (opt == INS_OPTS_SCALABLE_Q)
                {
                    fmt = IF_SVE_BR_3B;
                }
                else
                {
                    assert(isValidVectorElemsize(optGetSveElemsize(opt))); // xx
                    fmt = IF_SVE_AT_3A;
                }
            }
            else
            {
                assert(isPredicateRegister(reg1)); // DDDD
                assert(isPredicateRegister(reg2)); // NNNN
                assert(isPredicateRegister(reg3)); // MMMM
                fmt = IF_SVE_CI_3A;
            }
            break;

        case INS_sve_tbl:
            assert(insOptsScalableStandard(opt));
            assert(isVectorRegister(reg1));                        // ddddd
            assert(isVectorRegister(reg2));                        // nnnnn
            assert(isVectorRegister(reg3));                        // mmmmm
            assert(isValidVectorElemsize(optGetSveElemsize(opt))); // xx

            if (sopt == INS_SCALABLE_OPTS_WITH_VECTOR_PAIR)
            {
                fmt = IF_SVE_BZ_3A_A;
            }
            else
            {
                assert(insScalableOptsNone(sopt));
                fmt = IF_SVE_BZ_3A;
            }
            break;

        case INS_sve_tbx:
            assert(insOptsScalableStandard(opt));
            assert(isVectorRegister(reg1));                        // ddddd
            assert(isVectorRegister(reg2));                        // nnnnn
            assert(isVectorRegister(reg3));                        // mmmmm
            assert(isValidVectorElemsize(optGetSveElemsize(opt))); // xx
            fmt = IF_SVE_BZ_3A;
            break;

        case INS_sve_tbxq:
            assert(insOptsScalableStandard(opt));
            assert(isVectorRegister(reg1));                        // ddddd
            assert(isVectorRegister(reg2));                        // nnnnn
            assert(isVectorRegister(reg3));                        // mmmmm
            assert(isValidVectorElemsize(optGetSveElemsize(opt))); // xx
            fmt = IF_SVE_AT_3A;
            break;

        case INS_sve_sdot:
        case INS_sve_udot:
            assert(isVectorRegister(reg1)); // ddddd
            assert(isVectorRegister(reg2)); // nnnnn
            assert(isVectorRegister(reg3)); // mmmmm

            if (opt == INS_OPTS_SCALABLE_H)
            {
                fmt = IF_SVE_EF_3A;
            }
            else
            {
                fmt = IF_SVE_EH_3A;
                assert(insOptsScalableWords(opt));
                assert(isValidVectorElemsize(optGetSveElemsize(opt))); // xx
            }
            break;

        case INS_sve_usdot:
            assert(opt == INS_OPTS_SCALABLE_B);
            assert(isVectorRegister(reg1)); // ddddd
            assert(isVectorRegister(reg2)); // nnnnn
            assert(isVectorRegister(reg3)); // mmmmm
            fmt = IF_SVE_EI_3A;
            break;

        case INS_sve_smlalb:
        case INS_sve_smlalt:
        case INS_sve_umlalb:
        case INS_sve_umlalt:
        case INS_sve_smlslb:
        case INS_sve_smlslt:
        case INS_sve_umlslb:
        case INS_sve_umlslt:
            assert(insOptsScalableAtLeastHalf(opt));
            assert(isVectorRegister(reg1));                        // ddddd
            assert(isVectorRegister(reg2));                        // nnnnn
            assert(isVectorRegister(reg3));                        // mmmmm
            assert(isValidVectorElemsize(optGetSveElemsize(opt))); // xx
            fmt = IF_SVE_EL_3A;
            break;

        case INS_sve_sqrdmlah:
        case INS_sve_sqrdmlsh:
            assert(insOptsScalableStandard(opt));
            assert(isVectorRegister(reg1));                        // ddddd
            assert(isVectorRegister(reg2));                        // nnnnn
            assert(isVectorRegister(reg3));                        // mmmmm
            assert(isValidVectorElemsize(optGetSveElemsize(opt))); // xx
            fmt = IF_SVE_EM_3A;
            break;

        case INS_sve_sqdmlalbt:
        case INS_sve_sqdmlslbt:
            assert(insOptsScalableAtLeastHalf(opt));
            assert(isVectorRegister(reg1));                        // ddddd
            assert(isVectorRegister(reg2));                        // nnnnn
            assert(isVectorRegister(reg3));                        // mmmmm
            assert(isValidVectorElemsize(optGetSveElemsize(opt))); // xx
            fmt = IF_SVE_EL_3A;
            break;

        case INS_sve_sqdmlalb:
        case INS_sve_sqdmlalt:
        case INS_sve_sqdmlslb:
        case INS_sve_sqdmlslt:
            assert(insOptsScalableAtLeastHalf(opt));
            assert(isVectorRegister(reg1));                        // ddddd
            assert(isVectorRegister(reg2));                        // nnnnn
            assert(isVectorRegister(reg3));                        // mmmmm
            assert(isValidVectorElemsize(optGetSveElemsize(opt))); // xx
            fmt = IF_SVE_EL_3A;
            break;

        case INS_sve_sclamp:
        case INS_sve_uclamp:
            assert(insOptsScalableStandard(opt));
            assert(isVectorRegister(reg1));                        // ddddd
            assert(isVectorRegister(reg2));                        // nnnnn
            assert(isVectorRegister(reg3));                        // mmmmm
            assert(isValidVectorElemsize(optGetSveElemsize(opt))); // xx
            fmt = IF_SVE_AT_3A;
            break;

        case INS_sve_zipq1:
        case INS_sve_zipq2:
        case INS_sve_uzpq1:
        case INS_sve_uzpq2:
        case INS_sve_tblq:
            assert(insOptsScalableStandard(opt));
            assert(isVectorRegister(reg1));                        // ddddd
            assert(isVectorRegister(reg2));                        // nnnnn
            assert(isVectorRegister(reg3));                        // mmmmm
            assert(isValidVectorElemsize(optGetSveElemsize(opt))); // xx
            fmt = IF_SVE_EX_3A;
            break;

        case INS_sve_saddlb:
        case INS_sve_saddlt:
        case INS_sve_uaddlb:
        case INS_sve_uaddlt:
        case INS_sve_ssublb:
        case INS_sve_ssublt:
        case INS_sve_usublb:
        case INS_sve_usublt:
        case INS_sve_sabdlb:
        case INS_sve_sabdlt:
        case INS_sve_uabdlb:
        case INS_sve_uabdlt:
            assert(insOptsScalableAtLeastHalf(opt));
            assert(isVectorRegister(reg1));                        // ddddd
            assert(isVectorRegister(reg2));                        // nnnnn
            assert(isVectorRegister(reg3));                        // mmmmm
            assert(isValidVectorElemsize(optGetSveElemsize(opt))); // xx
            fmt = IF_SVE_FL_3A;
            break;

        case INS_sve_saddwb:
        case INS_sve_saddwt:
        case INS_sve_uaddwb:
        case INS_sve_uaddwt:
        case INS_sve_ssubwb:
        case INS_sve_ssubwt:
        case INS_sve_usubwb:
        case INS_sve_usubwt:
            assert(insOptsScalableAtLeastHalf(opt));
            assert(isVectorRegister(reg1));                        // ddddd
            assert(isVectorRegister(reg2));                        // nnnnn
            assert(isVectorRegister(reg3));                        // mmmmm
            assert(isValidVectorElemsize(optGetSveElemsize(opt))); // xx
            fmt = IF_SVE_FM_3A;
            break;

        case INS_sve_smullb:
        case INS_sve_smullt:
        case INS_sve_umullb:
        case INS_sve_umullt:
        case INS_sve_sqdmullb:
        case INS_sve_sqdmullt:
            assert(insOptsScalableAtLeastHalf(opt));
            assert(isVectorRegister(reg1));                        // ddddd
            assert(isVectorRegister(reg2));                        // nnnnn
            assert(isVectorRegister(reg3));                        // mmmmm
            assert(isValidVectorElemsize(optGetSveElemsize(opt))); // xx
            fmt = IF_SVE_FL_3A;
            break;

        case INS_sve_pmullb:
        case INS_sve_pmullt:
            assert(isVectorRegister(reg1)); // ddddd
            assert(isVectorRegister(reg2)); // nnnnn
            assert(isVectorRegister(reg3)); // mmmmm

            if (opt == INS_OPTS_SCALABLE_Q)
            {
                fmt = IF_SVE_FN_3B;
            }
            else
            {
                assert((opt == INS_OPTS_SCALABLE_H) || (opt == INS_OPTS_SCALABLE_D));
                assert(isValidVectorElemsize(optGetSveElemsize(opt))); // xx
                fmt = IF_SVE_FL_3A;
            }
            break;

        case INS_sve_smmla:
        case INS_sve_usmmla:
        case INS_sve_ummla:
            assert(opt == INS_OPTS_SCALABLE_S);
            assert(isVectorRegister(reg1)); // ddddd
            assert(isVectorRegister(reg2)); // nnnnn
            assert(isVectorRegister(reg3)); // mmmmm
            fmt = IF_SVE_FO_3A;
            break;

        case INS_sve_rax1:
        case INS_sve_sm4ekey:
            assert(isVectorRegister(reg1)); // ddddd
            assert(isVectorRegister(reg2)); // nnnnn
            assert(isVectorRegister(reg3)); // mmmmm

            if (ins == INS_sve_rax1)
            {
                assert(opt == INS_OPTS_SCALABLE_D);
            }
            else
            {
                assert(opt == INS_OPTS_SCALABLE_S);
            }

            fmt = IF_SVE_GJ_3A;
            break;

        case INS_sve_fmlalb:
        case INS_sve_fmlalt:
            assert(isVectorRegister(reg1)); // ddddd
            assert(isVectorRegister(reg2)); // nnnnn
            assert(isVectorRegister(reg3)); // mmmmm

            if (opt == INS_OPTS_SCALABLE_B)
            {
                unreached(); // TODO-SVE: Not yet supported.
                fmt = IF_SVE_GN_3A;
            }
            else
            {
                assert(opt == INS_OPTS_SCALABLE_H);
                fmt = IF_SVE_HB_3A;
            }
            break;

        case INS_sve_fmlslb:
        case INS_sve_fmlslt:
        case INS_sve_bfmlalb:
        case INS_sve_bfmlalt:
        case INS_sve_bfmlslb:
        case INS_sve_bfmlslt:
            assert(opt == INS_OPTS_SCALABLE_H);
            assert(isVectorRegister(reg1)); // ddddd
            assert(isVectorRegister(reg2)); // nnnnn
            assert(isVectorRegister(reg3)); // mmmmm
            fmt = IF_SVE_HB_3A;
            break;

        case INS_sve_bfmmla:
            assert(opt == INS_OPTS_SCALABLE_H);
            assert(isVectorRegister(reg1)); // ddddd
            assert(isVectorRegister(reg2)); // nnnnn
            assert(isVectorRegister(reg3)); // mmmmm
            fmt = IF_SVE_HD_3A;
            break;

        case INS_sve_fmmla:
            unreached(); // TODO-SVE: Not yet supported.
            assert(opt == INS_OPTS_SCALABLE_D);
            assert(isVectorRegister(reg1)); // ddddd
            assert(isVectorRegister(reg2)); // nnnnn
            assert(isVectorRegister(reg3)); // mmmmm
            fmt = IF_SVE_HD_3A_A;
            break;

        case INS_sve_fmlallbb:
        case INS_sve_fmlallbt:
        case INS_sve_fmlalltb:
        case INS_sve_fmlalltt:
            unreached(); // TODO-SVE: Not yet supported.
            assert(opt == INS_OPTS_SCALABLE_B);
            assert(isVectorRegister(reg1)); // ddddd
            assert(isVectorRegister(reg2)); // nnnnn
            assert(isVectorRegister(reg3)); // mmmmm
            fmt = IF_SVE_GO_3A;
            break;

        case INS_sve_bfclamp:
            assert(opt == INS_OPTS_SCALABLE_H);
            assert(isVectorRegister(reg1)); // ddddd
            assert(isVectorRegister(reg2)); // nnnnn
            assert(isVectorRegister(reg3)); // mmmmm
            fmt = IF_SVE_GW_3B;
            break;

        case INS_sve_bfdot:
            assert(opt == INS_OPTS_SCALABLE_H);
            assert(isVectorRegister(reg1)); // ddddd
            assert(isVectorRegister(reg2)); // nnnnn
            assert(isVectorRegister(reg3)); // mmmmm
            fmt = IF_SVE_HA_3A;
            break;

        case INS_sve_fdot:
            assert(isVectorRegister(reg1)); // ddddd
            assert(isVectorRegister(reg2)); // nnnnn
            assert(isVectorRegister(reg3)); // mmmmm

            if (opt == INS_OPTS_SCALABLE_H)
            {
                fmt = IF_SVE_HA_3A;
            }
            else if (opt == INS_OPTS_SCALABLE_B)
            {
                unreached(); // TODO-SVE: Not yet supported.
                fmt = IF_SVE_HA_3A_E;
            }
            else
            {
                unreached(); // TODO-SVE: Not yet supported.
                assert(insOptsNone(opt));
                fmt = IF_SVE_HA_3A_F;
            }
            break;

        case INS_sve_eorbt:
        case INS_sve_eortb:
            assert(insOptsScalableStandard(opt));
            assert(isVectorRegister(reg1));                        // ddddd
            assert(isVectorRegister(reg2));                        // nnnnn
            assert(isVectorRegister(reg3));                        // mmmmm
            assert(isValidVectorElemsize(optGetSveElemsize(opt))); // xx
            fmt = IF_SVE_AT_3A;
            break;

        case INS_sve_bext:
        case INS_sve_bdep:
        case INS_sve_bgrp:
            assert(insOptsScalableStandard(opt));
            assert(isVectorRegister(reg1));                        // ddddd
            assert(isVectorRegister(reg2));                        // nnnnn
            assert(isVectorRegister(reg3));                        // mmmmm
            assert(isValidVectorElemsize(optGetSveElemsize(opt))); // xx
            fmt = IF_SVE_AT_3A;
            break;

        case INS_sve_saddlbt:
        case INS_sve_ssublbt:
        case INS_sve_ssubltb:
            assert(insOptsScalableAtLeastHalf(opt));
            assert(isVectorRegister(reg1));                        // ddddd
            assert(isVectorRegister(reg2));                        // nnnnn
            assert(isVectorRegister(reg3));                        // mmmmm
            assert(isValidVectorElemsize(optGetSveElemsize(opt))); // xx
            fmt = IF_SVE_FL_3A;
            break;

        case INS_sve_saba:
        case INS_sve_uaba:
            assert(insOptsScalableStandard(opt));
            assert(isVectorRegister(reg1));                        // ddddd
            assert(isVectorRegister(reg2));                        // nnnnn
            assert(isVectorRegister(reg3));                        // mmmmm
            assert(isValidVectorElemsize(optGetSveElemsize(opt))); // xx
            fmt = IF_SVE_FW_3A;
            break;

        case INS_sve_sabalb:
        case INS_sve_sabalt:
        case INS_sve_uabalb:
        case INS_sve_uabalt:
            assert(insOptsScalableAtLeastHalf(opt));
            assert(isVectorRegister(reg1));                        // ddddd
            assert(isVectorRegister(reg2));                        // nnnnn
            assert(isVectorRegister(reg3));                        // mmmmm
            assert(isValidVectorElemsize(optGetSveElemsize(opt))); // xx
            fmt = IF_SVE_EL_3A;
            break;

        case INS_sve_addhnb:
        case INS_sve_addhnt:
        case INS_sve_raddhnb:
        case INS_sve_raddhnt:
        case INS_sve_subhnb:
        case INS_sve_subhnt:
        case INS_sve_rsubhnb:
        case INS_sve_rsubhnt:
            unreached(); // TODO-SVE: Not yet supported.
            assert(insOptsScalableWide(opt));
            assert(isVectorRegister(reg1));                        // ddddd
            assert(isVectorRegister(reg2));                        // nnnnn
            assert(isVectorRegister(reg3));                        // mmmmm
            assert(isValidVectorElemsize(optGetSveElemsize(opt))); // xx
            fmt = IF_SVE_GC_3A;
            break;

        case INS_sve_histseg:
            assert(opt == INS_OPTS_SCALABLE_B);
            assert(isVectorRegister(reg1));                        // ddddd
            assert(isVectorRegister(reg2));                        // nnnnn
            assert(isVectorRegister(reg3));                        // mmmmm
            assert(isValidVectorElemsize(optGetSveElemsize(opt))); // xx
            fmt = IF_SVE_GF_3A;
            break;

        case INS_sve_fclamp:
            assert(insOptsScalableAtLeastHalf(opt));
            assert(isVectorRegister(reg1));                        // ddddd
            assert(isVectorRegister(reg2));                        // nnnnn
            assert(isVectorRegister(reg3));                        // mmmmm
            assert(isValidVectorElemsize(optGetSveElemsize(opt))); // xx
            fmt = IF_SVE_AT_3A;
            break;

        case INS_sve_not:
            assert(insScalableOptsNone(sopt));
            if (isPredicateRegister(reg1))
            {
                assert(opt == INS_OPTS_SCALABLE_B);
                assert(isPredicateRegister(reg2)); // gggg
                assert(isPredicateRegister(reg3)); // NNNN
                fmt = IF_SVE_CZ_4A;
            }
            else
            {
                assert(isVectorRegister(reg1));
                assert(isLowPredicateRegister(reg2));
                assert(isVectorRegister(reg3));
                assert(insOptsScalableStandard(opt));
                fmt = IF_SVE_AP_3A;
            }
            break;

        case INS_sve_nots:
            assert(opt == INS_OPTS_SCALABLE_B);
            assert(isPredicateRegister(reg1)); // DDDD
            assert(isPredicateRegister(reg2)); // gggg
            assert(isPredicateRegister(reg3)); // NNNN
            fmt = IF_SVE_CZ_4A;
            break;

        case INS_sve_clz:
        case INS_sve_cls:
        case INS_sve_cnt:
        case INS_sve_cnot:
            assert(isVectorRegister(reg1));
            assert(isLowPredicateRegister(reg2));
            assert(isVectorRegister(reg3));
            assert(insOptsScalableStandard(opt));
            assert(insScalableOptsNone(sopt));
            fmt = IF_SVE_AP_3A;
            break;

        case INS_sve_fabs:
        case INS_sve_fneg:
            assert(isVectorRegister(reg1));
            assert(isLowPredicateRegister(reg2));
            assert(isVectorRegister(reg3));
            assert(insOptsScalableFloat(opt));
            assert(insScalableOptsNone(sopt));
            fmt = IF_SVE_AP_3A;
            break;

        case INS_sve_abs:
        case INS_sve_neg:
            assert(isVectorRegister(reg1));
            assert(isLowPredicateRegister(reg2));
            assert(isVectorRegister(reg3));
            assert(insOptsScalableStandard(opt));
            assert(insScalableOptsNone(sopt));
            fmt = IF_SVE_AQ_3A;
            break;

        case INS_sve_sxtb:
        case INS_sve_uxtb:
            assert(isVectorRegister(reg1));
            assert(isLowPredicateRegister(reg2));
            assert(isVectorRegister(reg3));
            assert(insOptsScalableAtLeastHalf(opt));
            assert(insScalableOptsNone(sopt));
            fmt = IF_SVE_AQ_3A;
            break;

        case INS_sve_sxth:
        case INS_sve_uxth:
            assert(isVectorRegister(reg1));
            assert(isLowPredicateRegister(reg2));
            assert(isVectorRegister(reg3));
            assert(insOptsScalableWords(opt));
            assert(insScalableOptsNone(sopt));
            fmt = IF_SVE_AQ_3A;
            break;

        case INS_sve_sxtw:
        case INS_sve_uxtw:
            assert(isVectorRegister(reg1));
            assert(isLowPredicateRegister(reg2));
            assert(isVectorRegister(reg3));
            assert(opt == INS_OPTS_SCALABLE_D);
            assert(insScalableOptsNone(sopt));
            fmt = IF_SVE_AQ_3A;
            break;

        case INS_sve_index:
            assert(isValidScalarDatasize(size));
            assert(isVectorRegister(reg1));
            assert(isGeneralRegisterOrZR(reg2));
            assert(isGeneralRegisterOrZR(reg3));
            assert(insOptsScalableStandard(opt));
            assert(insScalableOptsNone(sopt));
            fmt = IF_SVE_BA_3A;
            break;

        case INS_sve_sqdmulh:
        case INS_sve_sqrdmulh:
            assert(isScalableVectorSize(size));
            assert(isVectorRegister(reg1));
            assert(isVectorRegister(reg2));
            assert(isVectorRegister(reg3));
            assert(insOptsScalableStandard(opt));
            assert(insScalableOptsNone(sopt));
            fmt = IF_SVE_AT_3A;
            break;

        case INS_sve_ftssel:
            assert(isScalableVectorSize(size));
            assert(isVectorRegister(reg1));
            assert(isVectorRegister(reg2));
            assert(isVectorRegister(reg3));
            assert(insOptsScalableFloat(opt));
            assert(insScalableOptsNone(sopt));
            fmt = IF_SVE_AT_3A;
            break;

        case INS_sve_compact:
            assert(isVectorRegister(reg1));
            assert(isLowPredicateRegister(reg2));
            assert(isVectorRegister(reg3));
            assert(insOptsScalableWords(opt));
            assert(insScalableOptsNone(sopt));
            fmt = IF_SVE_CL_3A;
            break;

        case INS_sve_clasta:
        case INS_sve_clastb:
            assert(insOptsScalableStandard(opt));
            assert(isLowPredicateRegister(reg2));
            assert(isVectorRegister(reg3));
            if (isGeneralRegister(reg1))
            {
                assert(insScalableOptsNone(sopt));
                assert(isValidScalarDatasize(size));
                fmt = IF_SVE_CO_3A;
            }
            else if (sopt == INS_SCALABLE_OPTS_WITH_SIMD_SCALAR)
            {
                assert(isFloatReg(reg1));
                assert(isScalableVectorSize(size));
                fmt = IF_SVE_CN_3A;
            }
            else
            {
                assert(insScalableOptsNone(sopt));
                assert(isVectorRegister(reg1));
                fmt = IF_SVE_CM_3A;
            }
            break;

        case INS_sve_cpy:
        case INS_sve_mov:
            assert(insOptsScalableStandard(opt));
            if (isVectorRegister(reg1)) // ddddd
            {
                if (sopt == INS_SCALABLE_OPTS_PREDICATE_MERGE)
                {
                    assert(isPredicateRegister(reg2));
                    assert(isVectorRegister(reg3));
                    fmt = IF_SVE_CW_4A;
                }
                else if (sopt == INS_SCALABLE_OPTS_WITH_SIMD_SCALAR)
                {
                    assert(isLowPredicateRegister(reg2));
                    assert(isVectorRegister(reg3));
                    fmt = IF_SVE_CP_3A;
                    // MOV is an alias for CPY, and is always the preferred disassembly.
                    ins = INS_sve_mov;
                }
                else if (isLowPredicateRegister(reg2))
                {
                    assert(isGeneralRegisterOrSP(reg3));
                    assert(insScalableOptsNone(sopt));

                    fmt  = IF_SVE_CQ_3A;
                    reg3 = encodingSPtoZR(reg3);
                    // MOV is an alias for CPY, and is always the preferred disassembly.
                    ins = INS_sve_mov;
                }
                else
                {
                    assert(insScalableOptsNone(sopt));
                    assert(ins == INS_sve_mov);
                    assert(isVectorRegister(reg2)); // nnnnn
                    assert(isVectorRegister(reg3)); // mmmmm
                    fmt = IF_SVE_AU_3A;
                    // ORR is an alias for MOV, and is always the preferred disassembly.
                    ins = INS_sve_orr;
                }
            }
            else if (isPredicateRegister(reg3)) // NNNN
            {
                assert(opt == INS_OPTS_SCALABLE_B);
                assert(isPredicateRegister(reg1)); // DDDD
                assert(isPredicateRegister(reg2)); // gggg
                fmt = sopt == INS_SCALABLE_OPTS_PREDICATE_MERGE ? IF_SVE_CZ_4A_K : IF_SVE_CZ_4A;
                // MOV is an alias for CPY, and is always the preferred disassembly.
                ins = INS_sve_mov;
            }
            else
            {
                unreached();
            }
            break;

        case INS_sve_lasta:
        case INS_sve_lastb:
            assert(insOptsScalableStandard(opt));
            assert(isLowPredicateRegister(reg2));
            assert(isVectorRegister(reg3));
            if (isGeneralRegister(reg1))
            {
                assert(insScalableOptsNone(sopt));
                assert(isGeneralRegister(reg1));
                fmt = IF_SVE_CS_3A;
            }
            else if (sopt == INS_SCALABLE_OPTS_WITH_SIMD_SCALAR)
            {
                assert(isVectorRegister(reg1));
                fmt = IF_SVE_CR_3A;
            }
            break;

        case INS_sve_revd:
            assert(isVectorRegister(reg1));       // ddddd
            assert(isLowPredicateRegister(reg2)); // ggg
            assert(isVectorRegister(reg3));       // nnnnn
            fmt = IF_SVE_CT_3A;
            break;

        case INS_sve_rbit:
            assert(isVectorRegister(reg1));
            assert(isLowPredicateRegister(reg2));
            assert(isVectorRegister(reg3));
            assert(insOptsScalableStandard(opt));
            assert(insScalableOptsNone(sopt));
            fmt = IF_SVE_CU_3A;
            break;

        case INS_sve_revb:
            assert(isVectorRegister(reg1));
            assert(isLowPredicateRegister(reg2));
            assert(isVectorRegister(reg3));
            assert(insOptsScalableAtLeastHalf(opt));
            assert(insScalableOptsNone(sopt));
            fmt = IF_SVE_CU_3A;
            break;

        case INS_sve_revh:
            assert(isVectorRegister(reg1));
            assert(isLowPredicateRegister(reg2));
            assert(isVectorRegister(reg3));
            assert(insOptsScalableWords(opt));
            assert(insScalableOptsNone(sopt));
            fmt = IF_SVE_CU_3A;
            break;

        case INS_sve_revw:
            assert(isVectorRegister(reg1));
            assert(isLowPredicateRegister(reg2));
            assert(isVectorRegister(reg3));
            assert(opt == INS_OPTS_SCALABLE_D);
            assert(insScalableOptsNone(sopt));
            fmt = IF_SVE_CU_3A;
            break;

        case INS_sve_splice:
            assert(isVectorRegister(reg1));
            assert(isLowPredicateRegister(reg2));
            assert(isVectorRegister(reg3));
            assert(insOptsScalableStandard(opt));
            // TODO-SVE: We currently support only the destructive version of splice. Remove the following assert when
            // the constructive version is added, as described in https://github.com/dotnet/runtime/issues/103850.
            assert(sopt != INS_SCALABLE_OPTS_WITH_VECTOR_PAIR);
            fmt = (sopt == INS_SCALABLE_OPTS_WITH_VECTOR_PAIR) ? IF_SVE_CV_3A : IF_SVE_CV_3B;
            break;

        case INS_sve_brka:
        case INS_sve_brkb:
            assert(isPredicateRegister(reg1));
            assert(isPredicateRegister(reg2));
            assert(isPredicateRegister(reg3));
            assert(insOptsScalableStandard(opt));
            if (sopt == INS_SCALABLE_OPTS_PREDICATE_MERGE)
            {
                pmerge = true;
            }
            fmt = IF_SVE_DB_3A;
            break;

        case INS_sve_brkas:
        case INS_sve_brkbs:
            assert(opt == INS_OPTS_SCALABLE_B);
            assert(isPredicateRegister(reg1));
            assert(isPredicateRegister(reg2));
            assert(isPredicateRegister(reg3));
            fmt = IF_SVE_DB_3B;
            break;

        case INS_sve_brkn:
        case INS_sve_brkns:
            assert(insOptsScalable(opt));
            assert(isPredicateRegister(reg1)); // MMMM
            assert(isPredicateRegister(reg2)); // gggg
            assert(isPredicateRegister(reg3)); // NNNN
            fmt = IF_SVE_DC_3A;
            break;

        case INS_sve_cntp:
            assert(isScalableVectorSize(size));
            assert(isGeneralRegister(reg1));                       // ddddd
            assert(isPredicateRegister(reg2));                     // gggg
            assert(isPredicateRegister(reg3));                     // NNNN
            assert(isValidVectorElemsize(optGetSveElemsize(opt))); // xx
            fmt = IF_SVE_DK_3A;
            break;

        case INS_sve_shadd:
        case INS_sve_shsub:
        case INS_sve_shsubr:
        case INS_sve_srhadd:
        case INS_sve_uhadd:
        case INS_sve_uhsub:
        case INS_sve_uhsubr:
        case INS_sve_urhadd:
            assert(isVectorRegister(reg1));
            assert(isLowPredicateRegister(reg2));
            assert(isVectorRegister(reg3));
            assert(insOptsScalableStandard(opt));
            assert(insScalableOptsNone(sopt));
            fmt = IF_SVE_AA_3A;
            break;

        case INS_sve_sadalp:
        case INS_sve_uadalp:
            assert(isVectorRegister(reg1));
            assert(isLowPredicateRegister(reg2));
            assert(isVectorRegister(reg3));
            assert(insOptsScalableAtLeastHalf(opt));
            assert(insScalableOptsNone(sopt));
            fmt = IF_SVE_EQ_3A;
            break;

        case INS_sve_addp:
        case INS_sve_smaxp:
        case INS_sve_sminp:
        case INS_sve_umaxp:
        case INS_sve_uminp:
            assert(isVectorRegister(reg1));
            assert(isLowPredicateRegister(reg2));
            assert(isVectorRegister(reg3));
            assert(insOptsScalableStandard(opt));
            assert(insScalableOptsNone(sopt));
            fmt = IF_SVE_AA_3A;
            break;

        case INS_sve_sqabs:
        case INS_sve_sqneg:
            assert(isVectorRegister(reg1));
            assert(isLowPredicateRegister(reg2));
            assert(isVectorRegister(reg3));
            assert(insOptsScalableStandard(opt));
            assert(insScalableOptsNone(sopt));
            fmt = IF_SVE_ES_3A;
            break;

        case INS_sve_urecpe:
        case INS_sve_ursqrte:
            assert(isVectorRegister(reg1));
            assert(isLowPredicateRegister(reg2));
            assert(isVectorRegister(reg3));
            assert(opt == INS_OPTS_SCALABLE_S);
            assert(insScalableOptsNone(sopt));
            fmt = IF_SVE_ES_3A;
            break;

        case INS_sve_sqadd:
        case INS_sve_sqsub:
        case INS_sve_uqadd:
        case INS_sve_uqsub:
            assert(isVectorRegister(reg1));
            assert(isVectorRegister(reg3));
            assert(insOptsScalableStandard(opt));
            assert(isScalableVectorSize(size));
            assert(insScalableOptsNone(sopt));

            if (isVectorRegister(reg2))
            {
                fmt = IF_SVE_AT_3A;
            }
            else
            {
                assert(isLowPredicateRegister(reg2));
                fmt = IF_SVE_AA_3A;
            }
            break;

        case INS_sve_sqsubr:
        case INS_sve_suqadd:
        case INS_sve_uqsubr:
        case INS_sve_usqadd:
            assert(isVectorRegister(reg1));
            assert(isLowPredicateRegister(reg2));
            assert(isVectorRegister(reg3));
            assert(insOptsScalableStandard(opt));
            assert(insScalableOptsNone(sopt));
            assert(isScalableVectorSize(size));
            fmt = IF_SVE_AA_3A;
            break;

        case INS_sve_sqrshl:
        case INS_sve_sqrshlr:
        case INS_sve_sqshl:
        case INS_sve_sqshlr:
        case INS_sve_srshl:
        case INS_sve_srshlr:
        case INS_sve_uqrshl:
        case INS_sve_uqrshlr:
        case INS_sve_uqshl:
        case INS_sve_uqshlr:
        case INS_sve_urshl:
        case INS_sve_urshlr:
            assert(isVectorRegister(reg1));
            assert(isLowPredicateRegister(reg2));
            assert(isVectorRegister(reg3));
            assert(insOptsScalableStandard(opt));
            assert(insScalableOptsNone(sopt));
            fmt = IF_SVE_AA_3A;
            break;

        case INS_sve_fcvtnt:
        case INS_sve_fcvtlt:
            assert(insOptsConvertFloatStepwise(opt));
            FALLTHROUGH;
        case INS_sve_fcvtxnt:
        case INS_sve_bfcvtnt:
            assert(isVectorRegister(reg1));       // ddddd
            assert(isLowPredicateRegister(reg2)); // ggg
            assert(isVectorRegister(reg3));       // nnnnn
            fmt = IF_SVE_GQ_3A;
            break;

        case INS_sve_faddp:
        case INS_sve_fmaxnmp:
        case INS_sve_fmaxp:
        case INS_sve_fminnmp:
        case INS_sve_fminp:
            assert(isVectorRegister(reg1));
            assert(isLowPredicateRegister(reg2));
            assert(isVectorRegister(reg3));
            assert(insOptsScalableFloat(opt));
            assert(insScalableOptsNone(sopt));
            fmt = IF_SVE_GR_3A;
            break;

        case INS_sve_faddqv:
        case INS_sve_fmaxnmqv:
        case INS_sve_fminnmqv:
        case INS_sve_fmaxqv:
        case INS_sve_fminqv:
            unreached(); // TODO-SVE: Not yet supported.
            assert(isVectorRegister(reg1));
            assert(isLowPredicateRegister(reg2));
            assert(isVectorRegister(reg3));
            assert(insOptsScalableFloat(opt));
            assert(insScalableOptsNone(sopt));
            fmt = IF_SVE_GS_3A;
            break;

        case INS_sve_fmaxnmv:
        case INS_sve_fmaxv:
        case INS_sve_fminnmv:
        case INS_sve_fminv:
        case INS_sve_faddv:
            assert(isFloatReg(reg1));
            assert(isLowPredicateRegister(reg2));
            assert(isVectorRegister(reg3));
            assert(insOptsScalableFloat(opt));
            assert(isScalableVectorSize(size));
            assert(insScalableOptsNone(sopt));
            fmt = IF_SVE_HE_3A;
            break;

        case INS_sve_fadda:
            assert(isFloatReg(reg1));
            assert(isLowPredicateRegister(reg2));
            assert(isVectorRegister(reg3));
            assert(insOptsScalableFloat(opt));
            assert(isScalableVectorSize(size));
            assert(insScalableOptsNone(sopt));
            fmt = IF_SVE_HJ_3A;
            break;

        case INS_sve_frecps:
        case INS_sve_frsqrts:
        case INS_sve_ftsmul:
            assert(insOptsScalableAtLeastHalf(opt));
            assert(isVectorRegister(reg1));                        // ddddd
            assert(isVectorRegister(reg2));                        // nnnnn
            assert(isVectorRegister(reg3));                        // mmmmm
            assert(isValidVectorElemsize(optGetSveElemsize(opt))); // xx
            fmt = IF_SVE_AT_3A;
            break;

        case INS_sve_fadd:
        case INS_sve_fsub:
        case INS_sve_fmul:
            assert(insOptsScalableAtLeastHalf(opt));
            assert(isVectorRegister(reg1));                        // ddddd
            assert(isVectorRegister(reg3));                        // mmmmm
            assert(isValidVectorElemsize(optGetSveElemsize(opt))); // xx
            assert(insScalableOptsNone(sopt));

            if (isVectorRegister(reg2)) // nnnnn
            {
                fmt = IF_SVE_AT_3A;
            }
            else
            {
                assert(isLowPredicateRegister(reg2)); // ggg
                fmt = IF_SVE_HL_3A;
            }
            break;

        case INS_sve_fabd:
        case INS_sve_fdiv:
        case INS_sve_fdivr:
        case INS_sve_fmax:
        case INS_sve_fmaxnm:
        case INS_sve_fmin:
        case INS_sve_fminnm:
        case INS_sve_fmulx:
        case INS_sve_fscale:
        case INS_sve_fsubr:
            assert(isVectorRegister(reg1));
            assert(isLowPredicateRegister(reg2));
            assert(isVectorRegister(reg3));
            assert(insOptsScalableAtLeastHalf(opt));
            assert(insScalableOptsNone(sopt));
            fmt = IF_SVE_HL_3A;
            break;

        case INS_sve_famax:
        case INS_sve_famin:
            unreached(); // TODO-SVE: Not yet supported.
            assert(isVectorRegister(reg1));
            assert(isLowPredicateRegister(reg2));
            assert(isVectorRegister(reg3));
            assert(insOptsScalableFloat(opt));
            assert(insScalableOptsNone(sopt));
            fmt = IF_SVE_HL_3A;
            break;

        case INS_sve_bfmul:
        case INS_sve_bfadd:
        case INS_sve_bfsub:
        case INS_sve_bfmaxnm:
        case INS_sve_bfminnm:
        case INS_sve_bfmax:
        case INS_sve_bfmin:
            assert(opt == INS_OPTS_SCALABLE_H);
            assert(isVectorRegister(reg1)); // ddddd
            assert(isVectorRegister(reg3)); // mmmmm
            assert(insScalableOptsNone(sopt));

            if (isVectorRegister(reg2)) // nnnnn
            {
                fmt = IF_SVE_HK_3B;
            }
            else
            {
                assert(isLowPredicateRegister(reg2)); // ggg
                fmt = IF_SVE_HL_3B;
            }
            break;

        case INS_sve_bsl:
        case INS_sve_eor3:
        case INS_sve_bcax:
        case INS_sve_bsl1n:
        case INS_sve_bsl2n:
        case INS_sve_nbsl:
            assert(opt == INS_OPTS_SCALABLE_D);
            assert(isVectorRegister(reg1)); // ddddd
            assert(isVectorRegister(reg2)); // mmmmm
            assert(isVectorRegister(reg3)); // kkkkk
            fmt = IF_SVE_AV_3A;
            break;

        case INS_sve_frintn:
        case INS_sve_frintm:
        case INS_sve_frintp:
        case INS_sve_frintz:
        case INS_sve_frinta:
        case INS_sve_frintx:
        case INS_sve_frinti:
            assert(isVectorRegister(reg1));
            assert(isLowPredicateRegister(reg2));
            assert(isVectorRegister(reg3));
            assert(insOptsScalableFloat(opt));
            assert(insScalableOptsNone(sopt));
            fmt = IF_SVE_HQ_3A;
            break;

        case INS_sve_bfcvt:
            assert(isVectorRegister(reg1));       // ddddd
            assert(isLowPredicateRegister(reg2)); // ggg
            assert(isVectorRegister(reg3));       // nnnnn
            fmt = IF_SVE_HO_3A;
            break;

        case INS_sve_fcvt:
            assert(isVectorRegister(reg1));       // ddddd
            assert(isLowPredicateRegister(reg2)); // ggg
            assert(isVectorRegister(reg3));       // nnnnn
            fmt = IF_SVE_HO_3B;
            break;

        case INS_sve_fcvtx:
            assert(isVectorRegister(reg1));       // ddddd
            assert(isLowPredicateRegister(reg2)); // ggg
            assert(isVectorRegister(reg3));       // nnnnn
            fmt = IF_SVE_HO_3C;
            break;

        case INS_sve_fcvtzs:
        case INS_sve_fcvtzu:
            assert(insOptsScalableFloat(opt) || opt == INS_OPTS_H_TO_S || opt == INS_OPTS_H_TO_D ||
                   opt == INS_OPTS_S_TO_D || opt == INS_OPTS_D_TO_S);
            assert(isVectorRegister(reg1));       // ddddd
            assert(isLowPredicateRegister(reg2)); // ggg
            assert(isVectorRegister(reg3));       // nnnnn
            fmt = IF_SVE_HP_3B;
            break;

        case INS_sve_scvtf:
        case INS_sve_ucvtf:
            assert(insOptsScalableAtLeastHalf(opt) || opt == INS_OPTS_S_TO_H || opt == INS_OPTS_S_TO_D ||
                   opt == INS_OPTS_D_TO_H || opt == INS_OPTS_D_TO_S);
            assert(isVectorRegister(reg1));       // ddddd
            assert(isLowPredicateRegister(reg2)); // ggg
            assert(isVectorRegister(reg3));       // nnnnn
            fmt = IF_SVE_HS_3A;
            break;

        case INS_sve_frecpx:
        case INS_sve_fsqrt:
            assert(isVectorRegister(reg1));
            assert(isLowPredicateRegister(reg2));
            assert(isVectorRegister(reg3));
            assert(insOptsScalableFloat(opt));
            assert(insScalableOptsNone(sopt));
            fmt = IF_SVE_HR_3A;
            break;

        case INS_sve_whilege:
        case INS_sve_whilegt:
        case INS_sve_whilelt:
        case INS_sve_whilele:
        case INS_sve_whilehs:
        case INS_sve_whilehi:
        case INS_sve_whilelo:
        case INS_sve_whilels:
            assert(isGeneralRegister(reg2));                       // nnnnn
            assert(isGeneralRegister(reg3));                       // mmmmm
            assert(isValidVectorElemsize(optGetSveElemsize(opt))); // xx
            assert(insOptsScalableStandard(opt));

            if (insScalableOptsNone(sopt))
            {
                assert(isPredicateRegister(reg1));    // DDDD
                assert(isValidGeneralDatasize(size)); // X
                fmt = IF_SVE_DT_3A;
            }
            else if (insScalableOptsWithPredicatePair(sopt))
            {
                assert(isLowPredicateRegister(reg1)); // DDD
                assert(size == EA_8BYTE);
                fmt = IF_SVE_DX_3A;
            }
            else
            {
                assert(insScalableOptsWithVectorLength(sopt)); // l
                assert(isHighPredicateRegister(reg1));         // DDD
                assert(size == EA_8BYTE);
                vectorLength4x = (sopt == INS_SCALABLE_OPTS_VL_4X);
                fmt            = IF_SVE_DY_3A;
            }
            break;

        case INS_sve_whilewr:
        case INS_sve_whilerw:
            assert(insOptsScalableStandard(opt));
            assert(isPredicateRegister(reg1)); // DDDD
            assert(isGeneralRegister(reg2));   // nnnnn
            assert(size == EA_8BYTE);
            assert(isGeneralRegister(reg3));                       // mmmmm
            assert(isValidVectorElemsize(optGetSveElemsize(opt))); // xx
            assert(insScalableOptsNone(sopt));
            fmt = IF_SVE_DU_3A;
            break;

        case INS_sve_movs:
            assert(insOptsScalable(opt));
            assert(isPredicateRegister(reg1)); // DDDD
            assert(isPredicateRegister(reg2)); // gggg
            assert(isPredicateRegister(reg3)); // NNNN
            fmt = IF_SVE_CZ_4A;
            break;

        case INS_sve_adclb:
        case INS_sve_adclt:
        case INS_sve_sbclb:
        case INS_sve_sbclt:
            assert(insOptsScalableWords(opt));
            assert(isVectorRegister(reg1));                        // ddddd
            assert(isVectorRegister(reg2));                        // nnnnn
            assert(isVectorRegister(reg3));                        // mmmmm
            assert(isValidVectorElemsize(optGetSveElemsize(opt))); // x
            fmt = IF_SVE_FY_3A;
            break;

        case INS_sve_mlapt:
            unreached(); // TODO-SVE: Not yet supported.
            assert(insOptsNone(opt));
            assert(isVectorRegister(reg1)); // ddddd
            assert(isVectorRegister(reg2)); // nnnnn
            assert(isVectorRegister(reg3)); // mmmmm
            fmt = IF_SVE_EW_3A;
            break;

        case INS_sve_madpt:
            unreached(); // TODO-SVE: Not yet supported.
            assert(insOptsNone(opt));
            assert(isVectorRegister(reg1)); // ddddd
            assert(isVectorRegister(reg2)); // mmmmm
            assert(isVectorRegister(reg3)); // aaaaa
            fmt = IF_SVE_EW_3B;
            break;

        case INS_sve_fcmeq:
        case INS_sve_fcmge:
        case INS_sve_fcmgt:
        case INS_sve_fcmlt:
        case INS_sve_fcmle:
        case INS_sve_fcmne:
            assert(insOptsScalableAtLeastHalf(opt));
            assert(insScalableOptsNone(sopt));
            assert(isPredicateRegister(reg1));
            assert(isLowPredicateRegister(reg2));
            assert(isVectorRegister(reg3));
            assert(isScalableVectorSize(size));
            fmt = IF_SVE_HI_3A;
            break;

        case INS_sve_flogb:
            assert(insOptsScalableAtLeastHalf(opt));
            assert(insScalableOptsNone(sopt));
            assert(isVectorRegister(reg1));
            assert(isLowPredicateRegister(reg2));
            assert(isVectorRegister(reg3));
            assert(isScalableVectorSize(size));
            fmt = IF_SVE_HP_3A;
            break;

        case INS_sve_ld1b:
        case INS_sve_ld1sb:
        case INS_sve_ld1h:
        case INS_sve_ld1sh:
        case INS_sve_ld1w:
        case INS_sve_ld1sw:
        case INS_sve_ld1d:
        case INS_sve_ldnf1b:
        case INS_sve_ldnf1sb:
        case INS_sve_ldnf1h:
        case INS_sve_ldnf1sh:
        case INS_sve_ldnf1w:
        case INS_sve_ldnf1sw:
        case INS_sve_ldnf1d:
        case INS_sve_ldnt1b:
        case INS_sve_ldnt1h:
        case INS_sve_ldnt1w:
        case INS_sve_ldnt1d:
        case INS_sve_ld1rqb:
        case INS_sve_ld1rqh:
        case INS_sve_ld1rqw:
        case INS_sve_ld1rqd:
            return emitIns_R_R_R_I(ins, size, reg1, reg2, reg3, 0, opt);

        default:
            unreached();
            break;
    }

    assert(fmt != IF_NONE);

    instrDesc* id = emitNewInstr(attr);

    id->idIns(ins);
    id->idInsFmt(fmt);
    id->idInsOpt(opt);

    id->idReg1(reg1);
    id->idReg2(reg2);
    id->idReg3(reg3);

    if (pmerge)
    {
        id->idPredicateReg2Merge(pmerge);
    }
    else if (vectorLength4x)
    {
        id->idVectorLength4x(vectorLength4x);
    }

    dispIns(id);
    appendToCurIG(id);
}

/*****************************************************************************
 *
 *  Add a SVE instruction referencing three registers and a constant.
 */

void emitter::emitInsSve_R_R_R_I(instruction     ins,
                                 emitAttr        attr,
                                 regNumber       reg1,
                                 regNumber       reg2,
                                 regNumber       reg3,
                                 ssize_t         imm,
                                 insOpts         opt /* = INS_OPTS_NONE */,
                                 insScalableOpts sopt /* = INS_SCALABLE_OPTS_NONE */)
{
    emitAttr  size     = EA_SIZE(attr);
    emitAttr  elemsize = EA_UNKNOWN;
    insFormat fmt      = IF_NONE;

    /* Figure out the encoding format of the instruction */
    switch (ins)
    {
        case INS_sve_adr:
            assert(isVectorRegister(reg1)); // ddddd
            assert(isVectorRegister(reg2)); // nnnnn
            assert(isVectorRegister(reg3)); // mmmmm
            assert(isValidUimm<2>(imm));
            switch (opt)
            {
                case INS_OPTS_SCALABLE_S:
                case INS_OPTS_SCALABLE_D:
                    assert(sopt == INS_SCALABLE_OPTS_LSL_N);
                    fmt = IF_SVE_BH_3A;
                    break;
                case INS_OPTS_SCALABLE_D_SXTW:
                    fmt = IF_SVE_BH_3B;
                    break;
                case INS_OPTS_SCALABLE_D_UXTW:
                    fmt = IF_SVE_BH_3B_A;
                    break;
                default:
                    assert(!"invalid instruction");
                    break;
            }
            break;

        case INS_sve_cmpeq:
        case INS_sve_cmpgt:
        case INS_sve_cmpge:
        case INS_sve_cmpne:
        case INS_sve_cmple:
        case INS_sve_cmplt:
            assert(insScalableOptsNone(sopt));
            assert(insOptsScalableStandard(opt));
            assert(isPredicateRegister(reg1));    // DDDD
            assert(isLowPredicateRegister(reg2)); // ggg
            assert(isVectorRegister(reg3));       // nnnnn
            assert(isValidSimm<5>(imm));          // iiiii
            fmt = IF_SVE_CY_3A;
            break;

        case INS_sve_cmphi:
        case INS_sve_cmphs:
        case INS_sve_cmplo:
        case INS_sve_cmpls:
            assert(insScalableOptsNone(sopt));
            assert(insOptsScalableStandard(opt));
            assert(isPredicateRegister(reg1));    // DDDD
            assert(isLowPredicateRegister(reg2)); // ggg
            assert(isVectorRegister(reg3));       // nnnnn
            assert(isValidUimm<7>(imm));          // iiiii
            fmt = IF_SVE_CY_3B;
            break;

        case INS_sve_sdot:
        case INS_sve_udot:
            assert(insScalableOptsNone(sopt));
            assert(isVectorRegister(reg1));    // ddddd
            assert(isVectorRegister(reg2));    // nnnnn
            assert(isLowVectorRegister(reg3)); // mmmm

            if (opt == INS_OPTS_SCALABLE_H)
            {
                assert(isValidUimm<2>(imm)); // ii
                fmt = IF_SVE_EG_3A;
            }
            else if (opt == INS_OPTS_SCALABLE_S)
            {
                assert((REG_V0 <= reg3) && (reg3 <= REG_V7)); // mmm
                assert(isValidUimm<2>(imm));                  // ii
                fmt = IF_SVE_EY_3A;
            }
            else
            {
                assert(opt == INS_OPTS_SCALABLE_D);
                assert(isValidUimm<1>(imm)); // i
                opt = INS_OPTS_SCALABLE_H;
                fmt = IF_SVE_EY_3B;
            }
            break;

        case INS_sve_usdot:
        case INS_sve_sudot:
            assert(insScalableOptsNone(sopt));
            assert(opt == INS_OPTS_SCALABLE_B);
            assert(isVectorRegister(reg1)); // ddddd
            assert(isVectorRegister(reg2)); // nnnnn
            assert(isVectorRegister(reg3)); // mmm
            assert((REG_V0 <= reg3) && (reg3 <= REG_V7));
            assert(isValidUimm<2>(imm)); // ii
            fmt = IF_SVE_EZ_3A;
            break;

        case INS_sve_mul:
            assert(insScalableOptsNone(sopt));
            assert(insOptsScalableAtLeastHalf(opt));
            assert(isVectorRegister(reg1));    // ddddd
            assert(isVectorRegister(reg2));    // nnnnn
            assert(isLowVectorRegister(reg3)); // mmmm

            switch (opt)
            {
                case INS_OPTS_SCALABLE_H:
                    assert(isValidUimm<3>(imm));                  // iii
                    assert((REG_V0 <= reg3) && (reg3 <= REG_V7)); // mmm
                    fmt = IF_SVE_FD_3A;
                    break;

                case INS_OPTS_SCALABLE_S:
                    assert(isValidUimm<2>(imm));                  // ii
                    assert((REG_V0 <= reg3) && (reg3 <= REG_V7)); // mmm
                    fmt = IF_SVE_FD_3B;
                    break;

                case INS_OPTS_SCALABLE_D:
                    assert(isValidUimm<1>(imm)); // i
                    fmt = IF_SVE_FD_3C;
                    break;

                default:
                    unreached();
                    break;
            }
            break;

        case INS_sve_cdot:
            assert(insScalableOptsNone(sopt));
            assert(insOptsScalableWords(opt));
            assert(isVectorRegister(reg1));                        // ddddd
            assert(isVectorRegister(reg2));                        // nnnnn
            assert(isVectorRegister(reg3));                        // mmmmm
            assert(isValidRot(imm));                               // rr
            assert(isValidVectorElemsize(optGetSveElemsize(opt))); // xx

            // Convert rot to bitwise representation
            imm = emitEncodeRotationImm0_to_270(imm);
            fmt = IF_SVE_EJ_3A;
            break;

        case INS_sve_cmla:
        case INS_sve_sqrdcmlah:
            assert(insScalableOptsNone(sopt));
            assert(insOptsScalableStandard(opt));
            assert(isVectorRegister(reg1));                        // ddddd
            assert(isVectorRegister(reg2));                        // nnnnn
            assert(isVectorRegister(reg3));                        // mmmmm
            assert(isValidRot(imm));                               // rr
            assert(isValidVectorElemsize(optGetSveElemsize(opt))); // xx

            // Convert rot to bitwise representation
            imm = emitEncodeRotationImm0_to_270(imm);
            fmt = IF_SVE_EK_3A;
            break;

        case INS_sve_ld1d:
            assert(insScalableOptsNone(sopt));
            assert(insOptsScalable(opt));
            assert(isVectorRegister(reg1));
            assert(isLowPredicateRegister(reg2));

            if (isGeneralRegister(reg3))
            {
                assert(isValidSimm<4>(imm));
                if (opt == INS_OPTS_SCALABLE_Q)
                {
                    fmt = IF_SVE_IH_3A_A;
                }
                else
                {
                    assert(opt == INS_OPTS_SCALABLE_D);
                    fmt = IF_SVE_IH_3A;
                }
            }
            else
            {
                assert(opt == INS_OPTS_SCALABLE_D);
                assert(isVectorRegister(reg3));
                assert((isValidUimm_MultipleOf<5, 8>(imm)));
                fmt = IF_SVE_IV_3A;
            }
            break;

        case INS_sve_ldff1d:
            assert(insScalableOptsNone(sopt));
            assert(opt == INS_OPTS_SCALABLE_D);
            assert(isVectorRegister(reg1));
            assert(isLowPredicateRegister(reg2));
            assert(isVectorRegister(reg3));
            assert((isValidUimm_MultipleOf<5, 8>(imm)));
            fmt = IF_SVE_IV_3A;
            break;

        case INS_sve_ld1w:
            assert(insScalableOptsNone(sopt));
            assert(insOptsScalableWordsOrQuadwords(opt));
            assert(isVectorRegister(reg1));
            assert(isLowPredicateRegister(reg2));

            if (isGeneralRegister(reg3))
            {
                assert(isValidSimm<4>(imm));
                fmt = IF_SVE_IH_3A_F;
            }
            else
            {
                assert(insOptsScalableWords(opt));
                assert(isVectorRegister(reg3));
                assert((isValidUimm_MultipleOf<5, 4>(imm)));
                fmt = IF_SVE_HX_3A_E;
            }
            break;

        case INS_sve_ld1sw:
            assert(insScalableOptsNone(sopt));
            assert(opt == INS_OPTS_SCALABLE_D);
            assert(isVectorRegister(reg1));
            assert(isLowPredicateRegister(reg2));

            if (isGeneralRegister(reg3))
            {
                assert(isValidSimm<4>(imm));
                fmt = IF_SVE_IJ_3A;
            }
            else
            {
                assert(isVectorRegister(reg3));
                assert((isValidUimm_MultipleOf<5, 4>(imm)));
                fmt = IF_SVE_IV_3A;
            }
            break;

        case INS_sve_ldff1sw:
            assert(insScalableOptsNone(sopt));
            assert(opt == INS_OPTS_SCALABLE_D);
            assert(isVectorRegister(reg1));
            assert(isLowPredicateRegister(reg2));
            assert(isVectorRegister(reg3));
            assert((isValidUimm_MultipleOf<5, 4>(imm)));
            fmt = IF_SVE_IV_3A;
            break;

        case INS_sve_ld1sb:
            assert(insScalableOptsNone(sopt));
            assert(insOptsScalableAtLeastHalf(opt));
            assert(isVectorRegister(reg1));
            assert(isLowPredicateRegister(reg2));

            if (isGeneralRegister(reg3))
            {
                assert(isGeneralRegister(reg3));
                assert(isValidSimm<4>(imm));
                fmt = IF_SVE_IJ_3A_D;
            }
            else
            {
                assert(insOptsScalableWords(opt));
                assert(isVectorRegister(reg3));
                assert(isValidUimm<5>(imm));
                fmt = IF_SVE_HX_3A_B;
            }
            break;

        case INS_sve_ld1b:
            assert(insScalableOptsNone(sopt));
            assert(insOptsScalableStandard(opt));
            assert(isVectorRegister(reg1));
            assert(isLowPredicateRegister(reg2));

            if (isGeneralRegister(reg3))
            {
                assert(isValidSimm<4>(imm));
                fmt = IF_SVE_IJ_3A_E;
            }
            else
            {
                assert(insOptsScalableWords(opt));
                assert(isVectorRegister(reg3));
                assert(isValidUimm<5>(imm));
                fmt = IF_SVE_HX_3A_B;
            }
            break;

        case INS_sve_ldff1b:
        case INS_sve_ldff1sb:
            assert(insScalableOptsNone(sopt));
            assert(insOptsScalableWords(opt));
            assert(isVectorRegister(reg1));
            assert(isLowPredicateRegister(reg2));
            assert(isVectorRegister(reg3));
            assert(isValidUimm<5>(imm));
            fmt = IF_SVE_HX_3A_B;
            break;

        case INS_sve_ld1sh:
            assert(insScalableOptsNone(sopt));
            assert(insOptsScalableWords(opt));
            assert(isVectorRegister(reg1));
            assert(isLowPredicateRegister(reg2));

            if (isGeneralRegister(reg3))
            {
                assert(isValidSimm<4>(imm));
                fmt = IF_SVE_IJ_3A_F;
            }
            else
            {
                assert(isVectorRegister(reg3));
                assert((isValidUimm_MultipleOf<5, 2>(imm)));
                fmt = IF_SVE_HX_3A_E;
            }
            break;

        case INS_sve_ld1h:
            assert(insScalableOptsNone(sopt));
            assert(insOptsScalableAtLeastHalf(opt));
            assert(isVectorRegister(reg1));
            assert(isLowPredicateRegister(reg2));

            if (isGeneralRegister(reg3))
            {
                assert(isValidSimm<4>(imm));
                fmt = IF_SVE_IJ_3A_G;
            }
            else
            {
                assert(isVectorRegister(reg3));
                assert((isValidUimm_MultipleOf<5, 2>(imm)));
                fmt = IF_SVE_HX_3A_E;
            }
            break;

        case INS_sve_ldff1h:
        case INS_sve_ldff1sh:
            assert(insScalableOptsNone(sopt));
            assert(insOptsScalableWords(opt));
            assert(isVectorRegister(reg1));
            assert(isLowPredicateRegister(reg2));
            assert(isVectorRegister(reg3));
            assert((isValidUimm_MultipleOf<5, 2>(imm)));
            fmt = IF_SVE_HX_3A_E;
            break;

        case INS_sve_ldff1w:
            assert(insScalableOptsNone(sopt));
            assert(insOptsScalableWords(opt));
            assert(isVectorRegister(reg1));
            assert(isLowPredicateRegister(reg2));
            assert(isVectorRegister(reg3));
            assert((isValidUimm_MultipleOf<5, 4>(imm)));
            fmt = IF_SVE_HX_3A_E;
            break;

        case INS_sve_ldnf1sw:
        case INS_sve_ldnf1d:
            assert(insScalableOptsNone(sopt));
            assert(opt == INS_OPTS_SCALABLE_D);
            assert(isVectorRegister(reg1));
            assert(isLowPredicateRegister(reg2));
            assert(isGeneralRegister(reg3));
            assert(isValidSimm<4>(imm));
            fmt = IF_SVE_IL_3A;
            break;

        case INS_sve_ldnf1sh:
        case INS_sve_ldnf1w:
            assert(insScalableOptsNone(sopt));
            assert(insOptsScalableWords(opt));
            assert(isVectorRegister(reg1));
            assert(isLowPredicateRegister(reg2));
            assert(isGeneralRegister(reg3));
            assert(isValidSimm<4>(imm));
            fmt = IF_SVE_IL_3A_A;
            break;

        case INS_sve_ldnf1h:
        case INS_sve_ldnf1sb:
            assert(insScalableOptsNone(sopt));
            assert(insOptsScalableAtLeastHalf(opt));
            assert(isVectorRegister(reg1));
            assert(isLowPredicateRegister(reg2));
            assert(isGeneralRegister(reg3));
            assert(isValidSimm<4>(imm));
            fmt = IF_SVE_IL_3A_B;
            break;

        case INS_sve_ldnf1b:
            assert(insScalableOptsNone(sopt));
            assert(insOptsScalableStandard(opt));
            assert(isVectorRegister(reg1));
            assert(isPredicateRegister(reg2));
            assert(isGeneralRegister(reg3));
            assert(isValidSimm<4>(imm));
            fmt = IF_SVE_IL_3A_C;
            break;

        case INS_sve_ldnt1b:
        case INS_sve_ldnt1h:
        case INS_sve_ldnt1w:
        case INS_sve_ldnt1d:
            assert(insScalableOptsNone(sopt));
            assert(insOptsScalableStandard(opt));
            assert(isVectorRegister(reg1));
            assert(isLowPredicateRegister(reg2));
            assert(isGeneralRegister(reg3));
            assert(isValidSimm<4>(imm));

#ifdef DEBUG
            switch (ins)
            {
                case INS_sve_ldnt1b:
                    assert(opt == INS_OPTS_SCALABLE_B);
                    break;

                case INS_sve_ldnt1h:
                    assert(opt == INS_OPTS_SCALABLE_H);
                    break;

                case INS_sve_ldnt1w:
                    assert(opt == INS_OPTS_SCALABLE_S);
                    break;

                case INS_sve_ldnt1d:
                    assert(opt == INS_OPTS_SCALABLE_D);
                    break;

                default:
                    assert(!"Invalid instruction");
                    break;
            }
#endif // DEBUG

            fmt = IF_SVE_IM_3A;
            break;

        case INS_sve_ld1rqb:
        case INS_sve_ld1rob:
        case INS_sve_ld1rqh:
        case INS_sve_ld1roh:
        case INS_sve_ld1rqw:
        case INS_sve_ld1row:
        case INS_sve_ld1rqd:
        case INS_sve_ld1rod:
            assert(insScalableOptsNone(sopt));
            assert(insOptsScalableStandard(opt));
            assert(isVectorRegister(reg1));
            assert(isLowPredicateRegister(reg2));
            assert(isGeneralRegister(reg3));

#ifdef DEBUG
            switch (ins)
            {
                case INS_sve_ld1rqb:
                case INS_sve_ld1rqd:
                case INS_sve_ld1rqh:
                case INS_sve_ld1rqw:
                    assert((isValidSimm_MultipleOf<4, 16>(imm)));
                    break;

                case INS_sve_ld1rob:
                case INS_sve_ld1rod:
                case INS_sve_ld1roh:
                case INS_sve_ld1row:
                    assert((isValidSimm_MultipleOf<4, 32>(imm)));
                    break;

                default:
                    assert(!"Invalid instruction");
                    break;
            }

            switch (ins)
            {
                case INS_sve_ld1rqb:
                case INS_sve_ld1rob:
                    assert(opt == INS_OPTS_SCALABLE_B);
                    break;

                case INS_sve_ld1rqh:
                case INS_sve_ld1roh:
                    assert(opt == INS_OPTS_SCALABLE_H);
                    break;

                case INS_sve_ld1rqw:
                case INS_sve_ld1row:
                    assert(opt == INS_OPTS_SCALABLE_S);
                    break;

                case INS_sve_ld1rqd:
                case INS_sve_ld1rod:
                    assert(opt == INS_OPTS_SCALABLE_D);
                    break;

                default:
                    assert(!"Invalid instruction");
                    break;
            }
#endif // DEBUG

            fmt = IF_SVE_IO_3A;
            break;

        case INS_sve_ld2q:
        case INS_sve_ld3q:
        case INS_sve_ld4q:
            assert(insScalableOptsNone(sopt));
            assert(opt == INS_OPTS_SCALABLE_Q);
            assert(isVectorRegister(reg1));
            assert(isLowPredicateRegister(reg2));
            assert(isGeneralRegister(reg3));

#ifdef DEBUG
            switch (ins)
            {
                case INS_sve_ld2q:
                    assert((isValidSimm_MultipleOf<4, 2>(imm)));
                    break;

                case INS_sve_ld3q:
                    assert((isValidSimm_MultipleOf<4, 3>(imm)));
                    break;

                case INS_sve_ld4q:
                    assert((isValidSimm_MultipleOf<4, 4>(imm)));
                    break;

                default:
                    assert(!"Invalid instruction");
                    break;
            }
#endif // DEBUG

            fmt = IF_SVE_IQ_3A;
            break;

        case INS_sve_ld2b:
        case INS_sve_ld3b:
        case INS_sve_ld4b:
        case INS_sve_ld2h:
        case INS_sve_ld3h:
        case INS_sve_ld4h:
        case INS_sve_ld2w:
        case INS_sve_ld3w:
        case INS_sve_ld4w:
        case INS_sve_ld2d:
        case INS_sve_ld3d:
        case INS_sve_ld4d:
            assert(insScalableOptsNone(sopt));
            assert(insOptsScalableStandard(opt));
            assert(isVectorRegister(reg1));
            assert(isLowPredicateRegister(reg2));
            assert(isGeneralRegister(reg3));

#ifdef DEBUG
            switch (ins)
            {
                case INS_sve_ld2b:
                case INS_sve_ld2h:
                case INS_sve_ld2w:
                case INS_sve_ld2d:
                    assert((isValidSimm_MultipleOf<4, 2>(imm)));
                    break;

                case INS_sve_ld3b:
                case INS_sve_ld3h:
                case INS_sve_ld3w:
                case INS_sve_ld3d:
                    assert((isValidSimm_MultipleOf<4, 3>(imm)));
                    break;

                case INS_sve_ld4b:
                case INS_sve_ld4h:
                case INS_sve_ld4w:
                case INS_sve_ld4d:
                    assert((isValidSimm_MultipleOf<4, 4>(imm)));
                    break;

                default:
                    assert(!"Invalid instruction");
                    break;
            }

            switch (ins)
            {
                case INS_sve_ld2b:
                case INS_sve_ld3b:
                case INS_sve_ld4b:
                    assert(opt == INS_OPTS_SCALABLE_B);
                    break;

                case INS_sve_ld2h:
                case INS_sve_ld3h:
                case INS_sve_ld4h:
                    assert(opt == INS_OPTS_SCALABLE_H);
                    break;

                case INS_sve_ld2w:
                case INS_sve_ld3w:
                case INS_sve_ld4w:
                    assert(opt == INS_OPTS_SCALABLE_S);
                    break;

                case INS_sve_ld2d:
                case INS_sve_ld3d:
                case INS_sve_ld4d:
                    assert(opt == INS_OPTS_SCALABLE_D);
                    break;

                default:
                    assert(!"Invalid instruction");
                    break;
            }
#endif // DEBUG

            fmt = IF_SVE_IS_3A;
            break;

        case INS_sve_st2q:
        case INS_sve_st3q:
        case INS_sve_st4q:
            assert(insScalableOptsNone(sopt));
            assert(opt == INS_OPTS_SCALABLE_Q);
            assert(isVectorRegister(reg1));
            assert(isLowPredicateRegister(reg2));
            assert(isGeneralRegister(reg3));

#ifdef DEBUG
            switch (ins)
            {
                case INS_sve_st2q:
                    assert((isValidSimm_MultipleOf<4, 2>(imm)));
                    break;

                case INS_sve_st3q:
                    assert((isValidSimm_MultipleOf<4, 3>(imm)));
                    break;

                case INS_sve_st4q:
                    assert((isValidSimm_MultipleOf<4, 4>(imm)));
                    break;

                default:
                    assert(!"Invalid instruction");
                    break;
            }
#endif // DEBUG

            fmt = IF_SVE_JE_3A;
            break;

        case INS_sve_stnt1b:
        case INS_sve_stnt1h:
        case INS_sve_stnt1w:
        case INS_sve_stnt1d:
            assert(insScalableOptsNone(sopt));
            assert(insOptsScalableStandard(opt));
            assert(isVectorRegister(reg1));
            assert(isLowPredicateRegister(reg2));
            assert(isGeneralRegister(reg3));
            assert(isValidSimm<4>(imm));

#ifdef DEBUG
            switch (ins)
            {
                case INS_sve_stnt1b:
                    assert(opt == INS_OPTS_SCALABLE_B);
                    break;

                case INS_sve_stnt1h:
                    assert(opt == INS_OPTS_SCALABLE_H);
                    break;

                case INS_sve_stnt1w:
                    assert(opt == INS_OPTS_SCALABLE_S);
                    break;

                case INS_sve_stnt1d:
                    assert(opt == INS_OPTS_SCALABLE_D);
                    break;

                default:
                    assert(!"Invalid instruction");
                    break;
            }
#endif // DEBUG

            fmt = IF_SVE_JM_3A;
            break;

        case INS_sve_st1w:
        case INS_sve_st1d:
            assert(insScalableOptsNone(sopt));
            assert(isVectorRegister(reg1));
            assert(isLowPredicateRegister(reg2));

            if (isGeneralRegister(reg3))
            {
                assert(isValidSimm<4>(imm));

                if (opt == INS_OPTS_SCALABLE_Q && (ins == INS_sve_st1d))
                {
                    fmt = IF_SVE_JN_3C_D;
                }
                else
                {
                    if ((ins == INS_sve_st1w) && insOptsScalableWords(opt))
                    {
                        fmt = IF_SVE_JN_3B;
                    }
                    else
                    {
#if DEBUG
                        if (ins == INS_sve_st1w)
                        {
                            assert(opt == INS_OPTS_SCALABLE_Q);
                        }
                        else
                        {
                            assert(opt == INS_OPTS_SCALABLE_D);
                        }
#endif // DEBUG
                        fmt = IF_SVE_JN_3C;
                    }
                }
            }
            else
            {
                assert(isVectorRegister(reg3));
                if ((ins == INS_sve_st1w) && insOptsScalableWords(opt))
                {
                    assert((isValidUimm_MultipleOf<5, 4>(imm)));
                    fmt = IF_SVE_JI_3A_A;
                }
                else
                {
                    assert(ins == INS_sve_st1d);
                    assert((isValidUimm_MultipleOf<5, 8>(imm)));
                    fmt = IF_SVE_JL_3A;
                }
            }
            break;

        case INS_sve_st2b:
        case INS_sve_st3b:
        case INS_sve_st4b:
        case INS_sve_st2h:
        case INS_sve_st3h:
        case INS_sve_st4h:
        case INS_sve_st2w:
        case INS_sve_st3w:
        case INS_sve_st4w:
        case INS_sve_st2d:
        case INS_sve_st3d:
        case INS_sve_st4d:
            assert(insScalableOptsNone(sopt));
            assert(insOptsScalableStandard(opt));
            assert(isVectorRegister(reg1));
            assert(isLowPredicateRegister(reg2));
            assert(isGeneralRegister(reg3));

#ifdef DEBUG
            switch (ins)
            {
                case INS_sve_st2b:
                case INS_sve_st2h:
                case INS_sve_st2w:
                case INS_sve_st2d:
                    assert((isValidSimm_MultipleOf<4, 2>(imm)));
                    break;

                case INS_sve_st3b:
                case INS_sve_st3h:
                case INS_sve_st3w:
                case INS_sve_st3d:
                    assert((isValidSimm_MultipleOf<4, 3>(imm)));
                    break;

                case INS_sve_st4b:
                case INS_sve_st4h:
                case INS_sve_st4w:
                case INS_sve_st4d:
                    assert((isValidSimm_MultipleOf<4, 4>(imm)));
                    break;

                default:
                    assert(!"Invalid instruction");
                    break;
            }

            switch (ins)
            {
                case INS_sve_st2b:
                case INS_sve_st3b:
                case INS_sve_st4b:
                    assert(opt == INS_OPTS_SCALABLE_B);
                    break;

                case INS_sve_st2h:
                case INS_sve_st3h:
                case INS_sve_st4h:
                    assert(opt == INS_OPTS_SCALABLE_H);
                    break;

                case INS_sve_st2w:
                case INS_sve_st3w:
                case INS_sve_st4w:
                    assert(opt == INS_OPTS_SCALABLE_S);
                    break;

                case INS_sve_st2d:
                case INS_sve_st3d:
                case INS_sve_st4d:
                    assert(opt == INS_OPTS_SCALABLE_D);
                    break;

                default:
                    assert(!"Invalid instruction");
                    break;
            }
#endif // DEBUG

            fmt = IF_SVE_JO_3A;
            break;

        case INS_sve_st1b:
        case INS_sve_st1h:
            assert(insScalableOptsNone(sopt));
            assert(isVectorRegister(reg1));
            assert(isLowPredicateRegister(reg2));

            if (isGeneralRegister(reg3))
            {
                assert(isValidSimm<4>(imm));
                // st1h is reserved for scalable B
                assert((ins == INS_sve_st1h) ? insOptsScalableAtLeastHalf(opt) : insOptsScalableStandard(opt));
                fmt = IF_SVE_JN_3A;
            }
            else
            {
                assert(insOptsScalableWords(opt));
                assert(isVectorRegister(reg3));

#ifdef DEBUG
                switch (ins)
                {
                    case INS_sve_st1b:
                        assert(isValidUimm<5>(imm));
                        break;

                    case INS_sve_st1h:
                        assert((isValidUimm_MultipleOf<5, 2>(imm)));
                        break;

                    default:
                        assert(!"Invalid instruction");
                        break;
                }
#endif // DEBUG

                fmt = IF_SVE_JI_3A_A;
            }
            break;

        case INS_sve_fmla:
        case INS_sve_fmls:
            assert(insScalableOptsNone(sopt));
            assert(isVectorRegister(reg1));    // ddddd
            assert(isVectorRegister(reg2));    // nnnnn
            assert(isLowVectorRegister(reg3)); // mmmm

            if (opt == INS_OPTS_SCALABLE_S)
            {
                assert((REG_V0 <= reg3) && (reg3 <= REG_V7)); // mmm
                assert(isValidUimm<2>(imm));                  // ii
                fmt = IF_SVE_GU_3A;
            }
            else
            {
                assert(opt == INS_OPTS_SCALABLE_D);
                assert(isValidUimm<1>(imm)); // i
                fmt = IF_SVE_GU_3B;
            }
            break;

        case INS_sve_bfmla:
        case INS_sve_bfmls:
            assert(insScalableOptsNone(sopt));
            assert(opt == INS_OPTS_SCALABLE_H);
            assert(isVectorRegister(reg1)); // ddddd
            assert(isVectorRegister(reg2)); // nnnnn
            assert(isVectorRegister(reg3)); // mmm
            assert((REG_V0 <= reg3) && (reg3 <= REG_V7));
            assert(isValidUimm<3>(imm)); // i ii
            fmt = IF_SVE_GU_3C;
            break;

        case INS_sve_fmul:
            assert(insScalableOptsNone(sopt));
            assert(isVectorRegister(reg1));    // ddddd
            assert(isVectorRegister(reg2));    // nnnnn
            assert(isLowVectorRegister(reg3)); // mmmm

            if (opt == INS_OPTS_SCALABLE_S)
            {
                assert((REG_V0 <= reg3) && (reg3 <= REG_V7)); // mmm
                assert(isValidUimm<2>(imm));                  // ii
                fmt = IF_SVE_GX_3A;
            }
            else
            {
                assert(opt == INS_OPTS_SCALABLE_D);
                assert(isValidUimm<1>(imm)); // i
                fmt = IF_SVE_GX_3B;
            }
            break;

        case INS_sve_bfmul:
            assert(insScalableOptsNone(sopt));
            assert(opt == INS_OPTS_SCALABLE_H);
            assert(isVectorRegister(reg1)); // ddddd
            assert(isVectorRegister(reg2)); // nnnnn
            assert(isVectorRegister(reg3)); // mmm
            assert((REG_V0 <= reg3) && (reg3 <= REG_V7));
            assert(isValidUimm<3>(imm)); // i ii
            fmt = IF_SVE_GX_3C;
            break;

        case INS_sve_fdot:
            assert(insScalableOptsNone(sopt));
            assert(isVectorRegister(reg1)); // ddddd
            assert(isVectorRegister(reg2)); // nnnnn
            assert(isVectorRegister(reg3)); // mmm
            assert((REG_V0 <= reg3) && (reg3 <= REG_V7));
            assert(isValidUimm<2>(imm)); // ii

            if (opt == INS_OPTS_SCALABLE_B)
            {
                unreached();                 // TODO-SVE: Not yet supported.
                assert(isValidUimm<2>(imm)); // ii
                fmt = IF_SVE_GY_3B_D;
            }
            else if (opt == INS_OPTS_SCALABLE_H)
            {
                assert(isValidUimm<2>(imm)); // ii
                fmt = IF_SVE_GY_3B;
            }
            else
            {
                unreached(); // TODO-SVE: Not yet supported.
                assert(insOptsNone(opt));
                assert(isValidUimm<3>(imm)); // i ii

                // Simplify emitDispInsHelp logic by setting insOpt
                opt = INS_OPTS_SCALABLE_B;
                fmt = IF_SVE_GY_3A;
            }
            break;

        case INS_sve_bfdot:
            assert(insScalableOptsNone(sopt));
            assert(opt == INS_OPTS_SCALABLE_H);
            assert(isVectorRegister(reg1)); // ddddd
            assert(isVectorRegister(reg2)); // nnnnn
            assert(isVectorRegister(reg3)); // mmm
            assert((REG_V0 <= reg3) && (reg3 <= REG_V7));
            assert(isValidUimm<2>(imm)); // ii
            fmt = IF_SVE_GY_3B;
            break;

        case INS_sve_mla:
        case INS_sve_mls:
            assert(insScalableOptsNone(sopt));
            assert(isVectorRegister(reg1));    // ddddd
            assert(isVectorRegister(reg2));    // nnnnn
            assert(isLowVectorRegister(reg3)); // mmmm

            if (opt == INS_OPTS_SCALABLE_H)
            {
                assert((REG_V0 <= reg3) && (reg3 <= REG_V7)); // mmm
                assert(isValidUimm<3>(imm));                  // i ii
                fmt = IF_SVE_FF_3A;
            }
            else if (opt == INS_OPTS_SCALABLE_S)
            {
                assert((REG_V0 <= reg3) && (reg3 <= REG_V7)); // mmm
                assert(isValidUimm<2>(imm));                  // ii
                fmt = IF_SVE_FF_3B;
            }
            else
            {
                assert(opt == INS_OPTS_SCALABLE_D);
                assert(isValidUimm<1>(imm)); // i
                fmt = IF_SVE_FF_3C;
            }
            break;

        case INS_sve_smullb:
        case INS_sve_smullt:
        case INS_sve_umullb:
        case INS_sve_umullt:
            assert(insScalableOptsNone(sopt));
            assert(isVectorRegister(reg1));    // ddddd
            assert(isVectorRegister(reg2));    // nnnnn
            assert(isLowVectorRegister(reg3)); // mmmm

            if (opt == INS_OPTS_SCALABLE_H)
            {
                assert((REG_V0 <= reg3) && (reg3 <= REG_V7)); // mmm
                assert(isValidUimm<3>(imm));                  // ii i
                fmt = IF_SVE_FE_3A;
            }
            else
            {
                assert(opt == INS_OPTS_SCALABLE_S);
                assert(isValidUimm<2>(imm)); // i i
                fmt = IF_SVE_FE_3B;
            }
            break;

        case INS_sve_smlalb:
        case INS_sve_smlalt:
        case INS_sve_umlalb:
        case INS_sve_umlalt:
        case INS_sve_smlslb:
        case INS_sve_smlslt:
        case INS_sve_umlslb:
        case INS_sve_umlslt:
            assert(insScalableOptsNone(sopt));
            assert(isVectorRegister(reg1));    // ddddd
            assert(isVectorRegister(reg2));    // nnnnn
            assert(isLowVectorRegister(reg3)); // mmmm

            if (opt == INS_OPTS_SCALABLE_H)
            {
                assert((REG_V0 <= reg3) && (reg3 <= REG_V7)); // mmm
                assert(isValidUimm<3>(imm));                  // ii i
                fmt = IF_SVE_FG_3A;
            }
            else
            {
                assert(opt == INS_OPTS_SCALABLE_S);
                assert(isValidUimm<2>(imm)); // i i
                fmt = IF_SVE_FG_3B;
            }
            break;

        case INS_sve_sqdmullb:
        case INS_sve_sqdmullt:
            assert(insScalableOptsNone(sopt));
            assert(isVectorRegister(reg1));    // ddddd
            assert(isVectorRegister(reg2));    // nnnnn
            assert(isLowVectorRegister(reg3)); // mmmm

            if (opt == INS_OPTS_SCALABLE_H)
            {
                assert((REG_V0 <= reg3) && (reg3 <= REG_V7)); // mmm
                assert(isValidUimm<3>(imm));                  // ii i
                fmt = IF_SVE_FH_3A;
            }
            else
            {
                assert(opt == INS_OPTS_SCALABLE_S);
                assert(isValidUimm<2>(imm)); // i i
                fmt = IF_SVE_FH_3B;
            }
            break;

        case INS_sve_sqdmulh:
        case INS_sve_sqrdmulh:
            assert(insScalableOptsNone(sopt));
            assert(isVectorRegister(reg1));    // ddddd
            assert(isVectorRegister(reg2));    // nnnnn
            assert(isLowVectorRegister(reg3)); // mmmm

            if (opt == INS_OPTS_SCALABLE_H)
            {
                assert((REG_V0 <= reg3) && (reg3 <= REG_V7)); // mmm
                assert(isValidUimm<3>(imm));                  // ii i
                fmt = IF_SVE_FI_3A;
            }
            else if (opt == INS_OPTS_SCALABLE_S)
            {
                assert((REG_V0 <= reg3) && (reg3 <= REG_V7)); // mmm
                assert(isValidUimm<2>(imm));                  // ii
                fmt = IF_SVE_FI_3B;
            }
            else
            {
                assert(opt == INS_OPTS_SCALABLE_D);
                assert(isValidUimm<1>(imm)); // i
                fmt = IF_SVE_FI_3C;
            }
            break;

        case INS_sve_sqdmlalb:
        case INS_sve_sqdmlalt:
        case INS_sve_sqdmlslb:
        case INS_sve_sqdmlslt:
            assert(insScalableOptsNone(sopt));
            assert(isVectorRegister(reg1));    // ddddd
            assert(isVectorRegister(reg2));    // nnnnn
            assert(isLowVectorRegister(reg3)); // mmmm

            if (opt == INS_OPTS_SCALABLE_H)
            {
                assert((REG_V0 <= reg3) && (reg3 <= REG_V7)); // mmm
                assert(isValidUimm<3>(imm));                  // ii i
                fmt = IF_SVE_FJ_3A;
            }
            else
            {
                assert(opt == INS_OPTS_SCALABLE_S);
                assert(isValidUimm<2>(imm)); // ii
                fmt = IF_SVE_FJ_3B;
            }
            break;

        case INS_sve_sqrdmlah:
        case INS_sve_sqrdmlsh:
            assert(insScalableOptsNone(sopt));
            assert(isVectorRegister(reg1));    // ddddd
            assert(isVectorRegister(reg2));    // nnnnn
            assert(isLowVectorRegister(reg3)); // mmmm

            if (opt == INS_OPTS_SCALABLE_H)
            {
                assert((REG_V0 <= reg3) && (reg3 <= REG_V7)); // mmm
                assert(isValidUimm<3>(imm));                  // i ii
                fmt = IF_SVE_FK_3A;
            }
            else if (opt == INS_OPTS_SCALABLE_S)
            {
                assert((REG_V0 <= reg3) && (reg3 <= REG_V7)); // mmm
                assert(isValidUimm<2>(imm));                  // ii
                fmt = IF_SVE_FK_3B;
            }
            else
            {
                assert(opt == INS_OPTS_SCALABLE_D);
                assert(isValidUimm<1>(imm)); // i
                fmt = IF_SVE_FK_3C;
            }
            break;

        case INS_sve_fcadd:
            assert(insScalableOptsNone(sopt));
            assert(insOptsScalableAtLeastHalf(opt));
            assert(isVectorRegister(reg1));
            assert(isLowPredicateRegister(reg2));
            assert(isVectorRegister(reg3));
            assert(isScalableVectorSize(size));
            assert(emitIsValidEncodedRotationImm90_or_270(imm));
            fmt = IF_SVE_GP_3A;
            break;

        case INS_sve_ld1rd:
            assert(insScalableOptsNone(sopt));
            assert(opt == INS_OPTS_SCALABLE_D);
            assert(isVectorRegister(reg1));
            assert(isLowPredicateRegister(reg2));
            assert(isGeneralRegister(reg3));
            assert((isValidUimm_MultipleOf<6, 8>(imm)));
            fmt = IF_SVE_IC_3A;
            break;

        case INS_sve_ld1rsw:
            assert(insScalableOptsNone(sopt));
            assert(opt == INS_OPTS_SCALABLE_D);
            assert(isVectorRegister(reg1));
            assert(isLowPredicateRegister(reg2));
            assert(isGeneralRegister(reg3));
            assert((isValidUimm_MultipleOf<6, 4>(imm)));
            fmt = IF_SVE_IC_3A;
            break;

        case INS_sve_ld1rsh:
            assert(insScalableOptsNone(sopt));
            assert(insOptsScalableWords(opt));
            assert(isVectorRegister(reg1));
            assert(isLowPredicateRegister(reg2));
            assert(isGeneralRegister(reg3));
            assert((isValidUimm_MultipleOf<6, 2>(imm)));
            fmt = IF_SVE_IC_3A_A;
            break;

        case INS_sve_ld1rw:
            assert(insScalableOptsNone(sopt));
            assert(insOptsScalableWords(opt));
            assert(isVectorRegister(reg1));
            assert(isLowPredicateRegister(reg2));
            assert(isGeneralRegister(reg3));
            assert((isValidUimm_MultipleOf<6, 4>(imm)));
            fmt = IF_SVE_IC_3A_A;
            break;

        case INS_sve_ld1rh:
            assert(insScalableOptsNone(sopt));
            assert(insOptsScalableAtLeastHalf(opt));
            assert(isVectorRegister(reg1));
            assert(isLowPredicateRegister(reg2));
            assert(isGeneralRegister(reg3));
            assert((isValidUimm_MultipleOf<6, 2>(imm)));
            fmt = IF_SVE_IC_3A_B;
            break;

        case INS_sve_ld1rsb:
            assert(insScalableOptsNone(sopt));
            assert(insOptsScalableAtLeastHalf(opt));
            assert(isVectorRegister(reg1));
            assert(isLowPredicateRegister(reg2));
            assert(isGeneralRegister(reg3));
            assert(isValidUimm<6>(imm));
            fmt = IF_SVE_IC_3A_B;
            break;

        case INS_sve_ld1rb:
            assert(insScalableOptsNone(sopt));
            assert(insOptsScalableStandard(opt));
            assert(isVectorRegister(reg1));
            assert(isLowPredicateRegister(reg2));
            assert(isGeneralRegister(reg3));
            assert(isValidUimm<6>(imm));
            fmt = IF_SVE_IC_3A_C;
            break;

        case INS_sve_fmlalb:
        case INS_sve_fmlalt:
        case INS_sve_fmlslb:
        case INS_sve_fmlslt:
        case INS_sve_bfmlalb:
        case INS_sve_bfmlalt:
        case INS_sve_bfmlslb:
        case INS_sve_bfmlslt:
            assert(insScalableOptsNone(sopt));
            assert(opt == INS_OPTS_SCALABLE_H);
            assert(isVectorRegister(reg1)); // ddddd
            assert(isVectorRegister(reg2)); // nnnnn
            assert(isVectorRegister(reg3)); // mmm
            assert((REG_V0 <= reg3) && (reg3 <= REG_V7));
            assert(isValidUimm<3>(imm)); // ii i
            fmt = IF_SVE_GZ_3A;
            break;

        case INS_sve_luti2:
            assert(insScalableOptsNone(sopt));
            assert(isVectorRegister(reg1)); // ddddd
            assert(isVectorRegister(reg2)); // nnnnn
            assert(isVectorRegister(reg3)); // mmmmm

            if (opt == INS_OPTS_SCALABLE_H)
            {
                assert(isValidUimm<3>(imm)); // iii
                fmt = IF_SVE_GG_3B;
            }
            else
            {
                assert(opt == INS_OPTS_SCALABLE_B);
                assert(isValidUimm<2>(imm)); // ii i
                fmt = IF_SVE_GG_3A;
            }
            unreached();
            break;

        case INS_sve_luti4:
            assert(isVectorRegister(reg1)); // ddddd
            assert(isVectorRegister(reg2)); // nnnnn
            assert(isVectorRegister(reg3)); // mmmmm

            if (opt == INS_OPTS_SCALABLE_H)
            {
                assert(isValidUimm<2>(imm));

                if (sopt == INS_SCALABLE_OPTS_WITH_VECTOR_PAIR)
                {
                    fmt = IF_SVE_GH_3B;
                }
                else
                {
                    assert(insScalableOptsNone(sopt));
                    fmt = IF_SVE_GH_3B_B;
                }
            }
            else
            {
                assert(opt == INS_OPTS_SCALABLE_B);
                assert(insScalableOptsNone(sopt));
                assert(isValidUimm<1>(imm)); // i
                fmt = IF_SVE_GH_3A;
            }
            unreached();
            break;

        default:
            unreached();
            break;

    } // end switch (ins)
    assert(fmt != IF_NONE);

    instrDesc* id = emitNewInstrCns(attr, imm);

    id->idIns(ins);
    id->idInsFmt(fmt);
    id->idInsOpt(opt);

    id->idReg1(reg1);
    id->idReg2(reg2);
    id->idReg3(reg3);

    dispIns(id);
    appendToCurIG(id);
}

/*****************************************************************************
 *
 *  Add a SVE instruction referencing three registers and two constants.
 */

void emitter::emitInsSve_R_R_R_I_I(instruction ins,
                                   emitAttr    attr,
                                   regNumber   reg1,
                                   regNumber   reg2,
                                   regNumber   reg3,
                                   ssize_t     imm1,
                                   ssize_t     imm2,
                                   insOpts     opt)
{
    insFormat fmt = IF_NONE;
    ssize_t   imm;

    switch (ins)
    {
        case INS_sve_cdot:
            assert(isVectorRegister(reg1));    // ddddd
            assert(isVectorRegister(reg2));    // nnnnn
            assert(isLowVectorRegister(reg3)); // mmmm
            assert(isValidRot(imm2));          // rr
            // Convert imm2 from rotation value (0-270) to bitwise representation (0-3)
            imm = (imm1 << 2) | emitEncodeRotationImm0_to_270(imm2);

            if (opt == INS_OPTS_SCALABLE_B)
            {
                assert(isValidUimm<2>(imm1));                 // ii
                assert((REG_V0 <= reg3) && (reg3 <= REG_V7)); // mmm
                fmt = IF_SVE_FA_3A;
            }
            else
            {
                assert(opt == INS_OPTS_SCALABLE_H);
                assert(isValidUimm<1>(imm1)); // i
                fmt = IF_SVE_FA_3B;
            }
            break;

        case INS_sve_cmla:
            assert(isVectorRegister(reg1));    // ddddd
            assert(isVectorRegister(reg2));    // nnnnn
            assert(isLowVectorRegister(reg3)); // mmmm
            assert(isValidRot(imm2));          // rr
            // Convert imm2 from rotation value (0-270) to bitwise representation (0-3)
            imm = (imm1 << 2) | emitEncodeRotationImm0_to_270(imm2);

            if (opt == INS_OPTS_SCALABLE_H)
            {
                assert(isValidUimm<2>(imm1));                 // ii
                assert((REG_V0 <= reg3) && (reg3 <= REG_V7)); // mmm
                fmt = IF_SVE_FB_3A;
            }
            else
            {
                assert(opt == INS_OPTS_SCALABLE_S);
                assert(isValidUimm<1>(imm1)); // i
                fmt = IF_SVE_FB_3B;
            }
            break;

        case INS_sve_sqrdcmlah:
            assert(isVectorRegister(reg1));    // ddddd
            assert(isVectorRegister(reg2));    // nnnnn
            assert(isLowVectorRegister(reg3)); // mmmm
            assert(isValidRot(imm2));          // rr
            // Convert imm2 from rotation value (0-270) to bitwise representation (0-3)
            imm = (imm1 << 2) | emitEncodeRotationImm0_to_270(imm2);

            if (opt == INS_OPTS_SCALABLE_H)
            {
                assert(isValidUimm<2>(imm1));                 // ii
                assert((REG_V0 <= reg3) && (reg3 <= REG_V7)); // mmm
                fmt = IF_SVE_FC_3A;
            }
            else
            {
                assert(opt == INS_OPTS_SCALABLE_S);
                assert(isValidUimm<1>(imm1)); // i
                fmt = IF_SVE_FC_3B;
            }
            break;

        case INS_sve_fcmla:
            assert(opt == INS_OPTS_SCALABLE_S);
            assert(isVectorRegister(reg1));    // ddddd
            assert(isVectorRegister(reg2));    // nnnnn
            assert(isLowVectorRegister(reg3)); // mmmm
            assert(isValidUimm<1>(imm1));      // i
            assert(isValidRot(imm2));          // rr

            // Convert imm2 from rotation value (0-270) to bitwise representation (0-3)
            imm = (imm1 << 2) | emitEncodeRotationImm0_to_270(imm2);
            fmt = IF_SVE_GV_3A;
            break;

        default:
            unreached();
            break;
    }

    assert(fmt != IF_NONE);

    instrDesc* id = emitNewInstrCns(attr, imm);
    id->idIns(ins);
    id->idInsFmt(fmt);
    id->idInsOpt(opt);

    id->idReg1(reg1);
    id->idReg2(reg2);
    id->idReg3(reg3);

    dispIns(id);
    appendToCurIG(id);
}

/*****************************************************************************
 *
 *  Add a SVE instruction referencing four registers.
 */

void emitter::emitInsSve_R_R_R_R(instruction     ins,
                                 emitAttr        attr,
                                 regNumber       reg1,
                                 regNumber       reg2,
                                 regNumber       reg3,
                                 regNumber       reg4,
                                 insOpts         opt /* = INS_OPTS_NONE*/,
                                 insScalableOpts sopt /* = INS_SCALABLE_OPTS_NONE */)
{
    emitAttr  size = EA_SIZE(attr);
    insFormat fmt  = IF_NONE;

    /* Figure out the encoding format of the instruction */
    switch (ins)
    {
        case INS_sve_sel:
            assert(insScalableOptsNone(sopt));
            if (isVectorRegister(reg1))
            {
                if (reg1 == reg4)
                {
                    // mov is a preferred alias for sel
                    return emitInsSve_R_R_R(INS_sve_mov, attr, reg1, reg2, reg3, opt,
                                            INS_SCALABLE_OPTS_PREDICATE_MERGE);
                }
                assert(insOptsScalableStandard(opt));
                assert(isPredicateRegister(reg2)); // VVVV
                assert(isVectorRegister(reg3));    // nnnnn
                assert(isVectorRegister(reg4));    // mmmmm
                fmt = IF_SVE_CW_4A;
            }
            else
            {
                assert(opt == INS_OPTS_SCALABLE_B);
                assert(isPredicateRegister(reg1)); // dddd
                assert(isPredicateRegister(reg2)); // gggg
                assert(isPredicateRegister(reg3)); // nnnn
                assert(isPredicateRegister(reg4)); // mmmm
                fmt = IF_SVE_CZ_4A;
            }
            break;

        case INS_sve_cmpeq:
        case INS_sve_cmpgt:
        case INS_sve_cmpge:
        case INS_sve_cmphi:
        case INS_sve_cmphs:
        case INS_sve_cmpne:
        case INS_sve_cmple:
        case INS_sve_cmplo:
        case INS_sve_cmpls:
        case INS_sve_cmplt:
            assert(isPredicateRegister(reg1));    // DDDD
            assert(isLowPredicateRegister(reg2)); // ggg
            assert(isVectorRegister(reg3));       // nnnnn
            assert(isVectorRegister(reg4));       // mmmmm
            assert(isScalableVectorSize(attr));   // xx
            if (sopt == INS_SCALABLE_OPTS_WIDE)
            {
                assert(insOptsScalableWide(opt));
                fmt = IF_SVE_CX_4A_A;
            }
            else
            {
                assert(insScalableOptsNone(sopt));
                assert(insOptsScalableStandard(opt));
                fmt = IF_SVE_CX_4A;
            }
            break;

        case INS_sve_and:
        case INS_sve_orr:
        case INS_sve_eor:
        case INS_sve_ands:
        case INS_sve_bic:
        case INS_sve_orn:
        case INS_sve_bics:
        case INS_sve_eors:
        case INS_sve_nor:
        case INS_sve_nand:
        case INS_sve_orrs:
        case INS_sve_orns:
        case INS_sve_nors:
        case INS_sve_nands:
            assert(opt == INS_OPTS_SCALABLE_B);
            assert(isPredicateRegister(reg1)); // dddd
            assert(isPredicateRegister(reg2)); // gggg
            assert(isPredicateRegister(reg3)); // nnnn
            assert(isPredicateRegister(reg4)); // mmmm
            fmt = IF_SVE_CZ_4A;
            break;

        case INS_sve_brkpa:
        case INS_sve_brkpb:
        case INS_sve_brkpas:
        case INS_sve_brkpbs:
            assert(opt == INS_OPTS_SCALABLE_B);
            assert(isPredicateRegister(reg1)); // dddd
            assert(isPredicateRegister(reg2)); // gggg
            assert(isPredicateRegister(reg3)); // nnnn
            assert(isPredicateRegister(reg4)); // mmmm
            fmt = IF_SVE_DA_4A;
            break;

        case INS_sve_fcmeq:
        case INS_sve_fcmge:
        case INS_sve_facge:
        case INS_sve_fcmgt:
        case INS_sve_facgt:
        case INS_sve_fcmlt:
        case INS_sve_fcmle:
        case INS_sve_fcmne:
        case INS_sve_fcmuo:
        case INS_sve_facle:
        case INS_sve_faclt:
            assert(insOptsScalableFloat(opt));
            assert(isVectorRegister(reg3));       // nnnnn
            assert(isVectorRegister(reg4));       // mmmmm
            assert(isPredicateRegister(reg1));    // DDDD
            assert(isLowPredicateRegister(reg2)); // ggg
            assert(isScalableVectorSize(attr));   // xx
            fmt = IF_SVE_HT_4A;
            break;

        case INS_sve_match:
        case INS_sve_nmatch:
            assert(insOptsScalableAtMaxHalf(opt));
            assert(isPredicateRegister(reg1));    // DDDD
            assert(isLowPredicateRegister(reg2)); // ggg
            assert(isVectorRegister(reg3));       // nnnnn
            assert(isVectorRegister(reg4));       // mmmmm
            assert(isScalableVectorSize(attr));   // xx
            fmt = IF_SVE_GE_4A;
            break;

        case INS_sve_mla:
        case INS_sve_mls:
            assert(insOptsScalableStandard(opt));
            assert(isVectorRegister(reg1));       // ddddd
            assert(isLowPredicateRegister(reg2)); // ggg
            assert(isVectorRegister(reg3));       // nnnnn
            assert(isVectorRegister(reg4));       // mmmmm
            assert(isScalableVectorSize(size));
            fmt = IF_SVE_AR_4A;
            break;

        case INS_sve_histcnt:
            assert(insOptsScalableWords(opt));
            assert(isVectorRegister(reg1));                        // ddddd
            assert(isLowPredicateRegister(reg2));                  // ggg
            assert(isVectorRegister(reg3));                        // nnnnn
            assert(isVectorRegister(reg4));                        // mmmmm
            assert(isValidVectorElemsize(optGetSveElemsize(opt))); // xx
            fmt = IF_SVE_GI_4A;
            break;

        case INS_sve_fmla:
        case INS_sve_fmls:
        case INS_sve_fnmla:
        case INS_sve_fnmls:
            assert(insOptsScalableAtLeastHalf(opt));
            assert(isVectorRegister(reg1));                        // ddddd
            assert(isLowPredicateRegister(reg2));                  // ggg
            assert(isVectorRegister(reg3));                        // nnnnn
            assert(isVectorRegister(reg4));                        // mmmmm
            assert(isValidVectorElemsize(optGetSveElemsize(opt))); // xx
            fmt = IF_SVE_HU_4A;
            break;

        case INS_sve_mad:
        case INS_sve_msb:
            assert(insOptsScalableStandard(opt));
            assert(isVectorRegister(reg1));       // ddddd
            assert(isLowPredicateRegister(reg2)); // ggg
            assert(isVectorRegister(reg3));       // mmmmm
            assert(isVectorRegister(reg4));       // aaaaa
            assert(isScalableVectorSize(size));
            fmt = IF_SVE_AS_4A;
            break;

        case INS_sve_st1b:
            assert(isVectorRegister(reg1));
            assert(isPredicateRegister(reg2));
            assert(isGeneralRegister(reg3));
            assert(isScalableVectorSize(size));
            assert(insScalableOptsNone(sopt));

            if (insOptsScalableStandard(opt))
            {
                if (isGeneralRegister(reg4))
                {
                    fmt = IF_SVE_JD_4A;
                }
                else
                {
                    assert(isVectorRegister(reg4));
                    fmt = IF_SVE_JK_4B;
                }
            }
            else
            {
                assert(insOptsScalable32bitExtends(opt));
                switch (opt)
                {
                    case INS_OPTS_SCALABLE_S_UXTW:
                    case INS_OPTS_SCALABLE_S_SXTW:
                        fmt = IF_SVE_JK_4A_B;
                        break;

                    case INS_OPTS_SCALABLE_D_UXTW:
                    case INS_OPTS_SCALABLE_D_SXTW:
                        fmt = IF_SVE_JK_4A;
                        break;

                    default:
                        assert(!"Invalid options for scalable");
                        break;
                }
            }
            break;

        case INS_sve_st1h:
            assert(isVectorRegister(reg1));
            assert(isPredicateRegister(reg2));
            assert(isGeneralRegister(reg3));
            assert(isScalableVectorSize(size));

            if (insOptsScalableStandard(opt))
            {
                if (sopt == INS_SCALABLE_OPTS_LSL_N)
                {
                    if (isGeneralRegister(reg4))
                    {
                        // st1h is reserved for scalable B
                        assert((ins == INS_sve_st1h) ? insOptsScalableAtLeastHalf(opt) : true);
                        assert(sopt == INS_SCALABLE_OPTS_LSL_N);
                        fmt = IF_SVE_JD_4A;
                    }
                    else
                    {
                        assert(isVectorRegister(reg4));
                        fmt = IF_SVE_JJ_4B;
                    }
                }
                else
                {
                    assert(isVectorRegister(reg4));
                    assert(insScalableOptsNone(sopt));
                    fmt = IF_SVE_JJ_4B_E;
                }
            }
            else
            {
                assert(insOptsScalable32bitExtends(opt));
                switch (opt)
                {
                    case INS_OPTS_SCALABLE_S_UXTW:
                    case INS_OPTS_SCALABLE_S_SXTW:
                        if (insScalableOptsNone(sopt))
                        {
                            fmt = IF_SVE_JJ_4A_D;
                        }
                        else
                        {
                            assert(sopt == INS_SCALABLE_OPTS_MOD_N);
                            fmt = IF_SVE_JJ_4A;
                        }
                        break;

                    case INS_OPTS_SCALABLE_D_UXTW:
                    case INS_OPTS_SCALABLE_D_SXTW:
                        if (insScalableOptsNone(sopt))
                        {
                            fmt = IF_SVE_JJ_4A_C;
                        }
                        else
                        {
                            assert(sopt == INS_SCALABLE_OPTS_MOD_N);
                            fmt = IF_SVE_JJ_4A_B;
                        }
                        break;

                    default:
                        assert(!"Invalid options for scalable");
                        break;
                }
            }
            break;

        case INS_sve_st1w:
            assert(isVectorRegister(reg1));
            assert(isPredicateRegister(reg2));
            assert(isGeneralRegister(reg3));
            assert(isScalableVectorSize(size));

            if (insOptsScalableStandard(opt))
            {
                if (sopt == INS_SCALABLE_OPTS_LSL_N)
                {
                    if (isGeneralRegister(reg4))
                    {
                        fmt = IF_SVE_JD_4B;
                    }
                    else
                    {
                        assert(isVectorRegister(reg4));
                        fmt = IF_SVE_JJ_4B;
                    }
                }
                else
                {
                    assert(isVectorRegister(reg4));
                    assert(insScalableOptsNone(sopt));
                    fmt = IF_SVE_JJ_4B_E;
                }
            }
            else if (opt == INS_OPTS_SCALABLE_Q)
            {
                assert(isGeneralRegister(reg4));
                assert(sopt == INS_SCALABLE_OPTS_LSL_N);
                fmt = IF_SVE_JD_4C;
            }
            else
            {
                assert(insOptsScalable32bitExtends(opt));
                assert(isVectorRegister(reg4));
                switch (opt)
                {
                    case INS_OPTS_SCALABLE_S_UXTW:
                    case INS_OPTS_SCALABLE_S_SXTW:
                        if (insScalableOptsNone(sopt))
                        {
                            fmt = IF_SVE_JJ_4A_D;
                        }
                        else
                        {
                            assert(sopt == INS_SCALABLE_OPTS_MOD_N);
                            fmt = IF_SVE_JJ_4A;
                        }
                        break;

                    case INS_OPTS_SCALABLE_D_UXTW:
                    case INS_OPTS_SCALABLE_D_SXTW:
                        if (insScalableOptsNone(sopt))
                        {
                            fmt = IF_SVE_JJ_4A_C;
                        }
                        else
                        {
                            assert(sopt == INS_SCALABLE_OPTS_MOD_N);
                            fmt = IF_SVE_JJ_4A_B;
                        }
                        break;

                    default:
                        assert(!"Invalid options for scalable");
                        break;
                }
            }
            break;

        case INS_sve_st1d:
            assert(isVectorRegister(reg1));
            assert(isPredicateRegister(reg2));
            assert(isGeneralRegister(reg3));
            assert(isScalableVectorSize(size));

            if (isGeneralRegister(reg4))
            {
                assert(sopt == INS_SCALABLE_OPTS_LSL_N);
                if (opt == INS_OPTS_SCALABLE_Q)
                {
                    fmt = IF_SVE_JD_4C_A;
                }
                else
                {
                    assert(opt == INS_OPTS_SCALABLE_D);
                    fmt = IF_SVE_JD_4C;
                }
            }
            else
            {
                assert(isVectorRegister(reg4));

                if (opt == INS_OPTS_SCALABLE_D)
                {
                    if (sopt == INS_SCALABLE_OPTS_LSL_N)
                    {
                        fmt = IF_SVE_JJ_4B;
                    }
                    else
                    {
                        assert(insScalableOptsNone(sopt));
                        fmt = IF_SVE_JJ_4B_C;
                    }
                }
                else
                {
                    assert(insOptsScalable32bitExtends(opt));
                    switch (opt)
                    {
                        case INS_OPTS_SCALABLE_D_UXTW:
                        case INS_OPTS_SCALABLE_D_SXTW:
                            if (sopt == INS_SCALABLE_OPTS_MOD_N)
                            {
                                fmt = IF_SVE_JJ_4A;
                            }
                            else
                            {
                                assert(insScalableOptsNone(sopt));
                                fmt = IF_SVE_JJ_4A_B;
                            }
                            break;

                        default:
                            assert(!"Invalid options for scalable");
                            break;
                    }
                }
            }
            break;

        case INS_sve_ld1b:
        case INS_sve_ld1sb:
        case INS_sve_ldff1b:
        case INS_sve_ldff1sb:
            assert(isVectorRegister(reg1));
            assert(isPredicateRegister(reg2));
            assert(isGeneralRegister(reg3));
            assert(isScalableVectorSize(size));
            assert(insScalableOptsNone(sopt));

            if (isGeneralRegisterOrZR(reg4))
            {
                switch (ins)
                {
                    case INS_sve_ldff1b:
                        assert(insOptsScalableStandard(opt));
                        fmt = IF_SVE_IG_4A_E;
                        break;

                    case INS_sve_ldff1sb:
                        assert(insOptsScalableAtLeastHalf(opt));
                        fmt = IF_SVE_IG_4A_D;
                        break;

                    case INS_sve_ld1sb:
                        assert(insOptsScalableAtLeastHalf(opt));
                        fmt = IF_SVE_IK_4A_F;
                        break;

                    case INS_sve_ld1b:
                        assert(insOptsScalableStandard(opt));
                        fmt = IF_SVE_IK_4A_H;
                        break;

                    default:
                        assert(!"Invalid instruction");
                        break;
                }
            }
            else
            {
                assert(isVectorRegister(reg4));

                if (insOptsScalableDoubleWord32bitExtends(opt))
                {
                    fmt = IF_SVE_HW_4A;
                }
                else if (insOptsScalableSingleWord32bitExtends(opt))
                {
                    fmt = IF_SVE_HW_4A_A;
                }
                else
                {
                    assert(opt == INS_OPTS_SCALABLE_D);
                    fmt = IF_SVE_HW_4B;
                }
            }
            break;

        case INS_sve_ld1h:
        case INS_sve_ld1sh:
        case INS_sve_ldff1h:
        case INS_sve_ldff1sh:
        case INS_sve_ld1w:
        case INS_sve_ldff1w:
            assert(isVectorRegister(reg1));
            assert(isPredicateRegister(reg2));
            assert(isGeneralRegister(reg3));
            assert(isScalableVectorSize(size));

            if (isGeneralRegisterOrZR(reg4))
            {
                assert(sopt == INS_SCALABLE_OPTS_LSL_N);

                switch (ins)
                {
                    case INS_sve_ldff1h:
                        assert(insOptsScalableStandard(opt));
                        fmt = IF_SVE_IG_4A_G;
                        break;

                    case INS_sve_ldff1sh:
                    case INS_sve_ldff1w:
                        assert(insOptsScalableWords(opt));
                        fmt = IF_SVE_IG_4A_F;
                        break;

                    case INS_sve_ld1w:
                        assert(insOptsScalableWordsOrQuadwords(opt));
                        fmt = IF_SVE_II_4A_H;
                        break;

                    case INS_sve_ld1sh:
                        assert(insOptsScalableWords(opt));
                        fmt = IF_SVE_IK_4A_G;
                        break;

                    case INS_sve_ld1h:
                        assert(insOptsScalableAtLeastHalf(opt));
                        fmt = IF_SVE_IK_4A_I;
                        break;

                    default:
                        assert(!"Invalid instruction");
                        break;
                }
            }
            else
            {
                assert(isVectorRegister(reg4));

                if (insOptsScalableDoubleWord32bitExtends(opt))
                {
                    if (sopt == INS_SCALABLE_OPTS_MOD_N)
                    {
                        fmt = IF_SVE_HW_4A_A;
                    }
                    else
                    {
                        assert(insScalableOptsNone(sopt));
                        fmt = IF_SVE_HW_4A_B;
                    }
                }
                else if (insOptsScalableSingleWord32bitExtends(opt))
                {
                    if (sopt == INS_SCALABLE_OPTS_MOD_N)
                    {
                        fmt = IF_SVE_HW_4A;
                    }
                    else
                    {
                        assert(insScalableOptsNone(sopt));
                        fmt = IF_SVE_HW_4A_C;
                    }
                }
                else
                {
                    assert(opt == INS_OPTS_SCALABLE_D);
                    if (sopt == INS_SCALABLE_OPTS_LSL_N)
                    {
                        fmt = IF_SVE_HW_4B;
                    }
                    else
                    {
                        assert(insScalableOptsNone(sopt));
                        fmt = IF_SVE_HW_4B_D;
                    }
                }
            }
            break;

        case INS_sve_ld1d:
        case INS_sve_ld1sw:
        case INS_sve_ldff1d:
        case INS_sve_ldff1sw:
            assert(isVectorRegister(reg1));
            assert(isPredicateRegister(reg2));
            assert(isGeneralRegister(reg3));
            assert(isScalableVectorSize(size));

            if (isGeneralRegisterOrZR(reg4))
            {
                assert(sopt == INS_SCALABLE_OPTS_LSL_N);

                if (opt == INS_OPTS_SCALABLE_Q)
                {
                    assert(reg4 != REG_ZR);
                    assert(ins == INS_sve_ld1d);
                    fmt = IF_SVE_II_4A_B;
                }
                else
                {
                    assert(opt == INS_OPTS_SCALABLE_D);

                    switch (ins)
                    {
                        case INS_sve_ldff1d:
                        case INS_sve_ldff1sw:
                            fmt = IF_SVE_IG_4A;
                            break;

                        case INS_sve_ld1d:
                            assert(reg4 != REG_ZR);
                            fmt = IF_SVE_II_4A;
                            break;

                        case INS_sve_ld1sw:
                            assert(reg4 != REG_ZR);
                            fmt = IF_SVE_IK_4A;
                            break;

                        default:
                            assert(!"Invalid instruction");
                            break;
                    }
                }
            }
            else if (insOptsScalableDoubleWord32bitExtends(opt))
            {
                assert(isVectorRegister(reg4));

                if (sopt == INS_SCALABLE_OPTS_MOD_N)
                {
                    fmt = IF_SVE_IU_4A;
                }
                else
                {
                    assert(insScalableOptsNone(sopt));

                    if (ins == INS_sve_ld1d)
                    {
                        fmt = IF_SVE_IU_4A_C;
                    }
                    else
                    {
                        fmt = IF_SVE_IU_4A_A;
                    }
                }
            }
            else if (sopt == INS_SCALABLE_OPTS_LSL_N)
            {
                assert(isVectorRegister(reg4));
                assert(opt == INS_OPTS_SCALABLE_D);
                fmt = IF_SVE_IU_4B;
            }
            else
            {
                assert(isVectorRegister(reg4));
                assert(opt == INS_OPTS_SCALABLE_D);
                assert(insScalableOptsNone(sopt));

                if (ins == INS_sve_ld1d)
                {
                    fmt = IF_SVE_IU_4B_D;
                }
                else
                {
                    fmt = IF_SVE_IU_4B_B;
                }
            }
            break;

        case INS_sve_ldnt1b:
        case INS_sve_ldnt1h:
        case INS_sve_ldnt1w:
        case INS_sve_ldnt1d:
        case INS_sve_ldnt1sb:
        case INS_sve_ldnt1sh:
        case INS_sve_ldnt1sw:
            assert(isVectorRegister(reg1));
            assert(isPredicateRegister(reg2));
            assert(isScalableVectorSize(size));

            if (isGeneralRegister(reg3))
            {
                assert(isGeneralRegister(reg4));

#ifdef DEBUG
                switch (ins)
                {
                    case INS_sve_ldnt1b:
                        assert(opt == INS_OPTS_SCALABLE_B);
                        assert(insScalableOptsNone(sopt));
                        break;

                    case INS_sve_ldnt1h:
                        assert(opt == INS_OPTS_SCALABLE_H);
                        assert(sopt == INS_SCALABLE_OPTS_LSL_N);
                        break;

                    case INS_sve_ldnt1w:
                        assert(opt == INS_OPTS_SCALABLE_S);
                        assert(sopt == INS_SCALABLE_OPTS_LSL_N);
                        break;

                    case INS_sve_ldnt1d:
                        assert(opt == INS_OPTS_SCALABLE_D);
                        assert(sopt == INS_SCALABLE_OPTS_LSL_N);
                        break;

                    default:
                        assert(!"Invalid instruction");
                        break;
                }
#endif // DEBUG

                fmt = IF_SVE_IN_4A;
            }
            else if ((ins == INS_sve_ldnt1d) || (ins == INS_sve_ldnt1sw))
            {
                assert(insOptsScalableWords(opt));
                assert(isVectorRegister(reg3));
                assert(isGeneralRegisterOrZR(reg4));
                assert(insScalableOptsNone(sopt));
                assert(opt == INS_OPTS_SCALABLE_D);
                fmt = IF_SVE_IX_4A;
            }
            else
            {
                assert(insOptsScalableWords(opt));
                assert(isVectorRegister(reg3));
                assert(isGeneralRegisterOrZR(reg4));
                assert(insScalableOptsNone(sopt));

                if (opt == INS_OPTS_SCALABLE_S)
                {
                    fmt = IF_SVE_IF_4A;
                }
                else
                {
                    assert(opt == INS_OPTS_SCALABLE_D);
                    fmt = IF_SVE_IF_4A_A;
                }
            }
            break;

        case INS_sve_ld1rob:
        case INS_sve_ld1roh:
        case INS_sve_ld1row:
        case INS_sve_ld1rod:
        case INS_sve_ld1rqb:
        case INS_sve_ld1rqh:
        case INS_sve_ld1rqw:
        case INS_sve_ld1rqd:
            assert(isVectorRegister(reg1));
            assert(isPredicateRegister(reg2));
            assert(isGeneralRegister(reg3));
            assert(isGeneralRegister(reg4));
            assert(isScalableVectorSize(size));

#ifdef DEBUG
            switch (ins)
            {
                case INS_sve_ld1rob:
                case INS_sve_ld1rqb:
                    assert(opt == INS_OPTS_SCALABLE_B);
                    assert(insScalableOptsNone(sopt));
                    break;

                case INS_sve_ld1roh:
                case INS_sve_ld1rqh:
                    assert(opt == INS_OPTS_SCALABLE_H);
                    assert(sopt == INS_SCALABLE_OPTS_LSL_N);
                    break;

                case INS_sve_ld1row:
                case INS_sve_ld1rqw:
                    assert(opt == INS_OPTS_SCALABLE_S);
                    assert(sopt == INS_SCALABLE_OPTS_LSL_N);
                    break;

                case INS_sve_ld1rod:
                case INS_sve_ld1rqd:
                    assert(opt == INS_OPTS_SCALABLE_D);
                    assert(sopt == INS_SCALABLE_OPTS_LSL_N);
                    break;

                default:
                    assert(!"Invalid instruction");
                    break;
            }
#endif // DEBUG

            fmt = IF_SVE_IP_4A;
            break;

        case INS_sve_ld1q:
            assert(isVectorRegister(reg1));
            assert(isPredicateRegister(reg2));
            assert(isVectorRegister(reg3));
            assert(isGeneralRegisterOrZR(reg4));
            assert(isScalableVectorSize(size));
            assert(opt == INS_OPTS_SCALABLE_Q);
            assert(insScalableOptsNone(sopt));
            fmt = IF_SVE_IW_4A;
            break;

        case INS_sve_ld2q:
        case INS_sve_ld3q:
        case INS_sve_ld4q:
            assert(isVectorRegister(reg1));
            assert(isPredicateRegister(reg2));
            assert(isGeneralRegister(reg3));
            assert(isGeneralRegister(reg4));
            assert(isScalableVectorSize(size));
            assert(opt == INS_OPTS_SCALABLE_Q);
            assert(sopt == INS_SCALABLE_OPTS_LSL_N);
            fmt = IF_SVE_IR_4A;
            break;

        case INS_sve_ld2b:
        case INS_sve_ld3b:
        case INS_sve_ld4b:
        case INS_sve_ld2h:
        case INS_sve_ld3h:
        case INS_sve_ld4h:
        case INS_sve_ld2w:
        case INS_sve_ld3w:
        case INS_sve_ld4w:
        case INS_sve_ld2d:
        case INS_sve_ld3d:
        case INS_sve_ld4d:
            assert(isVectorRegister(reg1));
            assert(isPredicateRegister(reg2));
            assert(isGeneralRegister(reg3));
            assert(isGeneralRegister(reg4));
            assert(isScalableVectorSize(size));

#ifdef DEBUG
            switch (ins)
            {
                case INS_sve_ld2b:
                case INS_sve_ld3b:
                case INS_sve_ld4b:
                    assert(opt == INS_OPTS_SCALABLE_B);
                    assert(insScalableOptsNone(sopt));
                    break;

                case INS_sve_ld2h:
                case INS_sve_ld3h:
                case INS_sve_ld4h:
                    assert(opt == INS_OPTS_SCALABLE_H);
                    assert(sopt == INS_SCALABLE_OPTS_LSL_N);
                    break;

                case INS_sve_ld2w:
                case INS_sve_ld3w:
                case INS_sve_ld4w:
                    assert(opt == INS_OPTS_SCALABLE_S);
                    assert(sopt == INS_SCALABLE_OPTS_LSL_N);
                    break;

                case INS_sve_ld2d:
                case INS_sve_ld3d:
                case INS_sve_ld4d:
                    assert(opt == INS_OPTS_SCALABLE_D);
                    assert(sopt == INS_SCALABLE_OPTS_LSL_N);
                    break;

                default:
                    assert(!"Invalid instruction");
                    break;
            }
#endif // DEBUG

            fmt = IF_SVE_IT_4A;
            break;

        case INS_sve_st1q:
            assert(isVectorRegister(reg1));
            assert(isPredicateRegister(reg2));
            assert(isVectorRegister(reg3));
            assert(isGeneralRegisterOrZR(reg4));
            assert(isScalableVectorSize(size));
            assert(opt == INS_OPTS_SCALABLE_Q);
            assert(insScalableOptsNone(sopt));
            fmt = IF_SVE_IY_4A;
            break;

        case INS_sve_stnt1b:
        case INS_sve_stnt1h:
        case INS_sve_stnt1w:
        case INS_sve_stnt1d:
            assert(isVectorRegister(reg1));
            assert(isPredicateRegister(reg2));
            assert(isScalableVectorSize(size));

            if (isGeneralRegister(reg3))
            {
                assert(isGeneralRegister(reg4));
#ifdef DEBUG
                switch (ins)
                {
                    case INS_sve_stnt1b:
                        assert(opt == INS_OPTS_SCALABLE_B);
                        assert(insScalableOptsNone(sopt));
                        break;

                    case INS_sve_stnt1h:
                        assert(opt == INS_OPTS_SCALABLE_H);
                        assert(sopt == INS_SCALABLE_OPTS_LSL_N);
                        break;

                    case INS_sve_stnt1w:
                        assert(opt == INS_OPTS_SCALABLE_S);
                        assert(sopt == INS_SCALABLE_OPTS_LSL_N);
                        break;

                    case INS_sve_stnt1d:
                        assert(opt == INS_OPTS_SCALABLE_D);
                        assert(sopt == INS_SCALABLE_OPTS_LSL_N);
                        break;

                    default:
                        assert(!"Invalid instruction");
                        break;
                }
#endif // DEBUG
                fmt = IF_SVE_JB_4A;
            }
            else
            {
                assert(isVectorRegister(reg3));
                assert(isGeneralRegisterOrZR(reg4));
                assert(isScalableVectorSize(size));
                assert(insScalableOptsNone(sopt));

                if (opt == INS_OPTS_SCALABLE_S)
                {
                    fmt = IF_SVE_IZ_4A;
                }
                else
                {
                    assert(opt == INS_OPTS_SCALABLE_D);
                    if (ins == INS_sve_stnt1d)
                    {
                        fmt = IF_SVE_JA_4A;
                    }
                    else
                    {
                        fmt = IF_SVE_IZ_4A_A;
                    }
                }
            }
            break;

        case INS_sve_st2b:
        case INS_sve_st3b:
        case INS_sve_st4b:
        case INS_sve_st2h:
        case INS_sve_st3h:
        case INS_sve_st4h:
        case INS_sve_st2w:
        case INS_sve_st3w:
        case INS_sve_st4w:
        case INS_sve_st2d:
        case INS_sve_st3d:
        case INS_sve_st4d:
            assert(isVectorRegister(reg1));
            assert(isPredicateRegister(reg2));
            assert(isGeneralRegister(reg3));
            assert(isGeneralRegister(reg4));
            assert(isScalableVectorSize(size));

#ifdef DEBUG
            switch (ins)
            {
                case INS_sve_st2b:
                case INS_sve_st3b:
                case INS_sve_st4b:
                    assert(opt == INS_OPTS_SCALABLE_B);
                    assert(insScalableOptsNone(sopt));
                    break;

                case INS_sve_st2h:
                case INS_sve_st3h:
                case INS_sve_st4h:
                    assert(opt == INS_OPTS_SCALABLE_H);
                    assert(sopt == INS_SCALABLE_OPTS_LSL_N);
                    break;

                case INS_sve_st2w:
                case INS_sve_st3w:
                case INS_sve_st4w:
                    assert(opt == INS_OPTS_SCALABLE_S);
                    assert(sopt == INS_SCALABLE_OPTS_LSL_N);
                    break;

                case INS_sve_st2d:
                case INS_sve_st3d:
                case INS_sve_st4d:
                    assert(opt == INS_OPTS_SCALABLE_D);
                    assert(sopt == INS_SCALABLE_OPTS_LSL_N);
                    break;

                default:
                    assert(!"Invalid instruction");
                    break;
            }
#endif // DEBUG
            fmt = IF_SVE_JC_4A;
            break;

        case INS_sve_st2q:
        case INS_sve_st3q:
        case INS_sve_st4q:
            assert(isVectorRegister(reg1));
            assert(isPredicateRegister(reg2));
            assert(isGeneralRegister(reg3));
            assert(isGeneralRegister(reg4));
            assert(isScalableVectorSize(size));
            assert(opt == INS_OPTS_SCALABLE_Q);
            fmt = IF_SVE_JF_4A;
            break;

        case INS_sve_bfmla:
        case INS_sve_bfmls:
            assert(opt == INS_OPTS_SCALABLE_H);
            assert(insScalableOptsNone(sopt));
            assert(isVectorRegister(reg1));
            assert(isLowPredicateRegister(reg2));
            assert(isVectorRegister(reg3));
            assert(isVectorRegister(reg4));
            fmt = IF_SVE_HU_4B;
            break;

        case INS_sve_fmad:
        case INS_sve_fmsb:
        case INS_sve_fnmad:
        case INS_sve_fnmsb:
            assert(insOptsScalableAtLeastHalf(opt));
            assert(insScalableOptsNone(sopt));
            assert(isVectorRegister(reg1));
            assert(isLowPredicateRegister(reg2));
            assert(isVectorRegister(reg3));
            assert(isVectorRegister(reg4));
            fmt = IF_SVE_HV_4A;
            break;

        default:
            unreached();
            break;
    }
    assert(fmt != IF_NONE);

    // Use aliases.
    switch (ins)
    {
        case INS_sve_cmple:
            std::swap(reg3, reg4);
            ins = INS_sve_cmpge;
            break;
        case INS_sve_cmplo:
            std::swap(reg3, reg4);
            ins = INS_sve_cmphi;
            break;
        case INS_sve_cmpls:
            std::swap(reg3, reg4);
            ins = INS_sve_cmphs;
            break;
        case INS_sve_cmplt:
            std::swap(reg3, reg4);
            ins = INS_sve_cmpgt;
            break;
        case INS_sve_facle:
            std::swap(reg3, reg4);
            ins = INS_sve_facge;
            break;
        case INS_sve_faclt:
            std::swap(reg3, reg4);
            ins = INS_sve_facgt;
            break;
        case INS_sve_fcmle:
            std::swap(reg3, reg4);
            ins = INS_sve_fcmge;
            break;
        case INS_sve_fcmlt:
            std::swap(reg3, reg4);
            ins = INS_sve_fcmgt;
            break;
        default:
            break;
    }

    instrDesc* id = emitNewInstr(attr);

    id->idIns(ins);
    id->idInsFmt(fmt);
    id->idInsOpt(opt);

    id->idReg1(reg1);
    id->idReg2(reg2);
    id->idReg3(reg3);
    id->idReg4(reg4);

    dispIns(id);
    appendToCurIG(id);
}

/*****************************************************************************
 *
 *  Add a SVE instruction referencing four registers and a constant.
 */

void emitter::emitInsSve_R_R_R_R_I(instruction ins,
                                   emitAttr    attr,
                                   regNumber   reg1,
                                   regNumber   reg2,
                                   regNumber   reg3,
                                   regNumber   reg4,
                                   ssize_t     imm,
                                   insOpts     opt /* = INS_OPT_NONE*/)
{
    emitAttr  size = EA_SIZE(attr);
    insFormat fmt  = IF_NONE;

    /* Figure out the encoding format of the instruction */
    switch (ins)
    {
        case INS_sve_fcmla:
            assert(insOptsScalableAtLeastHalf(opt));
            assert(isVectorRegister(reg1));
            assert(isLowPredicateRegister(reg2));
            assert(isVectorRegister(reg3));
            assert(isVectorRegister(reg4));
            assert(isScalableVectorSize(size));
            assert(emitIsValidEncodedRotationImm0_to_270(imm));
            fmt = IF_SVE_GT_4A;
            break;

        case INS_sve_psel:
            unreached(); // TODO-SVE: Not yet supported.
            assert(insOptsScalableStandard(opt));
            assert(isPredicateRegister(reg1)); // DDDD
            assert(isPredicateRegister(reg2)); // NNNN
            assert(isPredicateRegister(reg3)); // MMMM
            assert(isGeneralRegister(reg4));   // vv
            assert((REG_R12 <= reg4) && (reg4 <= REG_R15));

            switch (opt)
            {
                case INS_OPTS_SCALABLE_B:
                    assert(isValidUimm<4>(imm));
                    break;

                case INS_OPTS_SCALABLE_H:
                    assert(isValidUimm<3>(imm));
                    break;

                case INS_OPTS_SCALABLE_S:
                    assert(isValidUimm<2>(imm));
                    break;

                case INS_OPTS_SCALABLE_D:
                    assert(isValidUimm<1>(imm));
                    break;

                default:
                    unreached();
                    break;
            }

            fmt = IF_SVE_DV_4A;
            break;

        default:
            unreached();
            break;
    }
    assert(fmt != IF_NONE);

    instrDesc* id = emitNewInstrCns(attr, imm);

    id->idIns(ins);
    id->idInsFmt(fmt);
    id->idInsOpt(opt);

    id->idReg1(reg1);
    id->idReg2(reg2);
    id->idReg3(reg3);
    id->idReg4(reg4);

    dispIns(id);
    appendToCurIG(id);
}

/*****************************************************************************
 *
 *  Add a SVE instruction referencing a register, a SVE Pattern.
 */

void emitter::emitIns_R_PATTERN(
    instruction ins, emitAttr attr, regNumber reg1, insOpts opt, insSvePattern pattern /* = SVE_PATTERN_ALL*/)
{
    insFormat fmt = IF_NONE;

    /* Figure out the encoding format of the instruction */
    switch (ins)
    {
        case INS_sve_ptrue:
        case INS_sve_ptrues:
            assert(isPredicateRegister(reg1));
            assert(isScalableVectorSize(attr));
            assert(insOptsScalableStandard(opt));
            fmt = IF_SVE_DE_1A;
            break;

        default:
            unreached();
            break;

    } // end switch (ins)
    assert(fmt != IF_NONE);

    instrDesc* id = emitNewInstr(attr);

    id->idIns(ins);
    id->idInsFmt(fmt);

    id->idReg1(reg1);
    id->idInsOpt(opt);
    id->idSvePattern(pattern);

    dispIns(id);
    appendToCurIG(id);
}

/*****************************************************************************
 *
 *  Add a SVE instruction referencing a register, a SVE Pattern and an immediate.
 */

void emitter::emitIns_R_PATTERN_I(instruction   ins,
                                  emitAttr      attr,
                                  regNumber     reg1,
                                  insSvePattern pattern,
                                  ssize_t       imm,
                                  insOpts       opt /* = INS_OPTS_NONE */)
{
    emitAttr  size = EA_SIZE(attr);
    insFormat fmt  = IF_NONE;

    /* Figure out the encoding format of the instruction */
    switch (ins)
    {
        case INS_sve_cntb:
        case INS_sve_cntd:
        case INS_sve_cnth:
        case INS_sve_cntw:
            assert(insOptsNone(opt));
            assert(isGeneralRegister(reg1));  // ddddd
            assert(isValidUimmFrom1<4>(imm)); // iiii
            assert(size == EA_8BYTE);
            fmt = IF_SVE_BL_1A;
            break;

        case INS_sve_incd:
        case INS_sve_inch:
        case INS_sve_incw:
        case INS_sve_decd:
        case INS_sve_dech:
        case INS_sve_decw:
            assert(isValidUimmFrom1<4>(imm)); // iiii

            if (insOptsNone(opt))
            {
                assert(isGeneralRegister(reg1)); // ddddd
                assert(size == EA_8BYTE);
                fmt = IF_SVE_BM_1A;
            }
            else
            {
                assert(insOptsScalableAtLeastHalf(opt));
                assert(isVectorRegister(reg1)); // ddddd
                fmt = IF_SVE_BN_1A;
            }
            break;

        case INS_sve_incb:
        case INS_sve_decb:
            assert(isGeneralRegister(reg1));  // ddddd
            assert(isValidUimmFrom1<4>(imm)); // iiii
            assert(size == EA_8BYTE);
            fmt = IF_SVE_BM_1A;
            break;

        case INS_sve_sqincb:
        case INS_sve_uqincb:
        case INS_sve_sqdecb:
        case INS_sve_uqdecb:
            assert(insOptsNone(opt));
            assert(isGeneralRegister(reg1));      // ddddd
            assert(isValidUimmFrom1<4>(imm));     // iiii
            assert(isValidGeneralDatasize(size)); // X
            fmt = IF_SVE_BO_1A;
            break;

        case INS_sve_sqinch:
        case INS_sve_uqinch:
        case INS_sve_sqdech:
        case INS_sve_uqdech:
        case INS_sve_sqincw:
        case INS_sve_uqincw:
        case INS_sve_sqdecw:
        case INS_sve_uqdecw:
        case INS_sve_sqincd:
        case INS_sve_uqincd:
        case INS_sve_sqdecd:
        case INS_sve_uqdecd:
            assert(isValidUimmFrom1<4>(imm)); // iiii

            if (insOptsNone(opt))
            {
                assert(isGeneralRegister(reg1));      // ddddd
                assert(isValidGeneralDatasize(size)); // X
                fmt = IF_SVE_BO_1A;
            }
            else
            {
                assert(insOptsScalableAtLeastHalf(opt));
                assert(isVectorRegister(reg1)); // ddddd
                assert(isScalableVectorSize(size));
                fmt = IF_SVE_BP_1A;
            }
            break;

        default:
            unreached();
            break;

    } // end switch (ins)
    assert(fmt != IF_NONE);

    instrDesc* id = emitNewInstrCns(attr, imm);

    id->idIns(ins);
    id->idInsFmt(fmt);
    id->idInsOpt(opt);
    id->idOpSize(size);

    id->idReg1(reg1);
    id->idSvePattern(pattern);

    dispIns(id);
    appendToCurIG(id);
}

/*****************************************************************************
 *
 *  Add a SVE instruction referencing three registers and a SVE 'prfop'.
 */

void emitter::emitIns_PRFOP_R_R_R(instruction     ins,
                                  emitAttr        attr,
                                  insSvePrfop     prfop,
                                  regNumber       reg1,
                                  regNumber       reg2,
                                  regNumber       reg3,
                                  insOpts         opt /* = INS_OPTS_NONE */,
                                  insScalableOpts sopt /* = INS_SCALABLE_OPTS_NONE */)
{
    emitAttr  size = EA_SIZE(attr);
    insFormat fmt  = IF_NONE;

    /* Figure out the encoding format of the instruction */
    switch (ins)
    {
        case INS_sve_prfb:
            assert(insScalableOptsNone(sopt));
            assert(isLowPredicateRegister(reg1));
            assert(isGeneralRegister(reg2));
            assert(isScalableVectorSize(size));

            if (insOptsScalable32bitExtends(opt))
            {
                assert(isVectorRegister(reg3));

                if (insOptsScalableSingleWord32bitExtends(opt))
                {
                    fmt = IF_SVE_HY_3A;
                }
                else
                {
                    assert(insOptsScalableDoubleWord32bitExtends(opt));
                    fmt = IF_SVE_HY_3A_A;
                }
            }
            else if (isVectorRegister(reg3))
            {
                assert(opt == INS_OPTS_SCALABLE_D);
                fmt = IF_SVE_HY_3B;
            }
            else
            {
                assert(insOptsNone(opt));
                assert(isGeneralRegister(reg3));
                fmt = IF_SVE_IB_3A;
            }
            break;

        case INS_sve_prfh:
        case INS_sve_prfw:
        case INS_sve_prfd:
            assert(isLowPredicateRegister(reg1));
            assert(isGeneralRegister(reg2));
            assert(isScalableVectorSize(size));

            if (sopt == INS_SCALABLE_OPTS_MOD_N)
            {
                if (insOptsScalableSingleWord32bitExtends(opt))
                {
                    fmt = IF_SVE_HY_3A;
                }
                else
                {
                    assert(insOptsScalableDoubleWord32bitExtends(opt));
                    fmt = IF_SVE_HY_3A_A;
                }
            }
            else
            {
                assert(sopt == INS_SCALABLE_OPTS_LSL_N);
                if (isVectorRegister(reg3))
                {
                    assert(opt == INS_OPTS_SCALABLE_D);
                    fmt = IF_SVE_HY_3B;
                }
                else
                {
                    assert(insOptsNone(opt));
                    assert(isGeneralRegister(reg3));
                    fmt = IF_SVE_IB_3A;
                }
            }
            break;

        default:
            unreached();
            break;

    } // end switch (ins)
    assert(fmt != IF_NONE);

    instrDesc* id = emitNewInstr(attr);

    id->idIns(ins);
    id->idInsOpt(opt);
    id->idInsFmt(fmt);

    id->idReg1(reg1);
    id->idReg2(reg2);
    id->idReg3(reg3);
    id->idSvePrfop(prfop);

    dispIns(id);
    appendToCurIG(id);
}

/*****************************************************************************
 *
 *  Add a SVE instruction referencing two registers, a SVE 'prfop' and an immediate.
 */

void emitter::emitIns_PRFOP_R_R_I(instruction ins,
                                  emitAttr    attr,
                                  insSvePrfop prfop,
                                  regNumber   reg1,
                                  regNumber   reg2,
                                  int         imm,
                                  insOpts     opt /* = INS_OPTS_NONE */)
{
    emitAttr  size = EA_SIZE(attr);
    insFormat fmt  = IF_NONE;

    /* Figure out the encoding format of the instruction */
    switch (ins)
    {
        case INS_sve_prfb:
        case INS_sve_prfh:
        case INS_sve_prfw:
        case INS_sve_prfd:
            assert(isLowPredicateRegister(reg1));
            assert(isScalableVectorSize(size));

            if (isVectorRegister(reg2))
            {
                assert(insOptsScalableWords(opt));

#ifdef DEBUG
                switch (ins)
                {
                    case INS_sve_prfb:
                        assert(isValidUimm<5>(imm));
                        break;

                    case INS_sve_prfh:
                        assert((isValidUimm_MultipleOf<5, 2>(imm)));
                        break;

                    case INS_sve_prfw:
                        assert((isValidUimm_MultipleOf<5, 4>(imm)));
                        break;

                    case INS_sve_prfd:
                        assert((isValidUimm_MultipleOf<5, 8>(imm)));
                        break;

                    default:
                        assert(!"Invalid instruction");
                        break;
                }
#endif // DEBUG
                fmt = IF_SVE_HZ_2A_B;
            }
            else
            {
                assert(insOptsNone(opt));
                assert(isGeneralRegister(reg2));
                assert(isValidSimm<6>(imm));
                fmt = IF_SVE_IA_2A;
            }
            break;

        default:
            unreached();
            break;

    } // end switch (ins)
    assert(fmt != IF_NONE);

    instrDesc* id = emitNewInstrCns(attr, imm);

    id->idIns(ins);
    id->idInsOpt(opt);
    id->idInsFmt(fmt);

    id->idReg1(reg1);
    id->idReg2(reg2);
    id->idSvePrfop(prfop);

    dispIns(id);
    appendToCurIG(id);
}

/*****************************************************************************
 *
 *  Returns the encoding to select the 1/2/4/8 byte elemsize for an Arm64 Sve vector instruction
 */

/*static*/ emitter::code_t emitter::insEncodeSveElemsize(emitAttr size)
{
    switch (size)
    {
        case EA_1BYTE:
            return 0x00000000;

        case EA_2BYTE:
            return 0x00400000; // set the bit at location 22

        case EA_4BYTE:
            return 0x00800000; // set the bit at location 23

        case EA_8BYTE:
            return 0x00C00000; // set the bit at location 23 and 22

        default:
            assert(!"Invalid insOpt for vector register");
    }
    return 0;
}

/*****************************************************************************
 *
 *  Returns the encoding to select the 1/2/4/8 byte elemsize for an Arm64 Sve vector instruction
 *  This specifically encodes the size at bit locations '22-21'.
 */

/*static*/ emitter::code_t emitter::insEncodeSveElemsize_22_to_21(emitAttr size)
{
    switch (size)
    {
        case EA_1BYTE:
            return 0;

        case EA_2BYTE:
            return (1 << 21); // set the bit at location 21

        case EA_4BYTE:
            return (1 << 22); // set the bit at location 22

        case EA_8BYTE:
            return (1 << 22) | (1 << 21); // set the bit at location 22 and 21

        default:
            assert(!"Invalid insOpt for vector register");
    }
    return 0;
}

/*****************************************************************************
 *
 *  Returns the encoding to select the 1/2/4/8 byte elemsize for an Arm64 Sve vector instruction
 *  This specifically encodes the size at bit locations '18-17'.
 */

/*static*/ emitter::code_t emitter::insEncodeSveElemsize_18_to_17(emitAttr size)
{
    switch (size)
    {
        case EA_1BYTE:
            return 0;

        case EA_2BYTE:
            return (1 << 17); // set the bit at location 17

        case EA_4BYTE:
            return (1 << 18); // set the bit at location 18

        case EA_8BYTE:
            return (1 << 18) | (1 << 17); // set the bit at location 18 and 17

        default:
            assert(!"Invalid insOpt for vector register");
    }
    return 0;
}

/*****************************************************************************
 *
 *  Returns the encoding to select the 4/8 byte elemsize for an Arm64 Sve vector instruction
 *  This specifically encodes the field 'sz' at bit location '20'.
 */

/*static*/ emitter::code_t emitter::insEncodeSveElemsize_sz_20(emitAttr size)
{
    switch (size)
    {
        case EA_4BYTE:
            return 0;

        case EA_8BYTE:
            return (1 << 20);

        default:
            assert(!"Invalid insOpt for vector register");
    }
    return 0;
}

/*****************************************************************************
 *
 *  Returns the encoding to select the 4/8 byte elemsize for an Arm64 Sve vector instruction
 *  This specifically encodes the field 'sz' at bit location '21'.
 */

/*static*/ emitter::code_t emitter::insEncodeSveElemsize_sz_21(emitAttr size)
{
    switch (size)
    {
        case EA_4BYTE:
            return 0;

        case EA_8BYTE:
            return (1 << 21);

        default:
            assert(!"Invalid insOpt for vector register");
    }
    return 0;
}

/*****************************************************************************
 *
 *  Returns the encoding to select the 1/2/4/8 byte elemsize for an Arm64 Sve vector instruction
 *  This specifically encodes the field 'tszh:tszl' at bit locations '23-22:20-19'.
 */

/*static*/ emitter::code_t emitter::insEncodeSveElemsize_tszh_23_tszl_20_to_19(emitAttr size)
{
    switch (size)
    {
        case EA_1BYTE:
            return 0x080000; // set the bit at location 19

        case EA_2BYTE:
            return 0x100000; // set the bit at location 20

        case EA_4BYTE:
            return 0x400000; // set the bit at location 22

        case EA_8BYTE:
            return 0x800000; // set the bit at location 23

        default:
            assert(!"Invalid size for vector register");
    }
    return 0;
}

/*****************************************************************************
 *
 *  Returns the encoding to select the 4/8 byte elemsize for an Arm64 Sve vector instruction at bit location '30'.
 *  This only works on select formats.
 */

/*static*/ emitter::code_t emitter::insEncodeSveElemsize_30_or_21(insFormat fmt, emitAttr size)
{
    switch (fmt)
    {
        case IF_SVE_HX_3A_B:
        case IF_SVE_HX_3A_E:
            switch (size)
            {
                case EA_4BYTE:
                    return 0;

                case EA_8BYTE:
                    return (1 << 30);

                default:
                    break;
            }

            assert(!"Invalid size for vector register");
            return 0;

        case IF_SVE_IV_3A:
            assert(size == EA_8BYTE);
            return 0;

        case IF_SVE_JI_3A_A:
            switch (size)
            {
                case EA_4BYTE:
                    return (1 << 21);

                case EA_8BYTE:
                    return 0;

                default:
                    break;
            }

            assert(!"Invalid size for vector register");
            return 0;

        default:
            break;
    }

    assert(!"Unexpected instruction format");
    return 0;
}
/*****************************************************************************
 *
 *  Returns the encoding for the field 'i1:tszh:tszl' at bit locations '23-22:20-18'.
 */

/*static*/ emitter::code_t emitter::insEncodeSveElemsize_tszh_tszl_and_imm(const insOpts opt, const ssize_t imm)
{
    code_t encoding = 0;

    switch (opt)
    {
        case INS_OPTS_SCALABLE_B:
            assert(isValidUimm<4>(imm));
            encoding = 0x040000; // set the bit at location 18
            // encode immediate at location 23-22:20-19
            encoding |= ((imm & 0b1100) << 22);
            encoding |= ((imm & 0b11) << 19);
            break;

        case INS_OPTS_SCALABLE_H:
            assert(isValidUimm<3>(imm));
            encoding = 0x080000; // set the bit at location 19
            // encode immediate at location 23-22:20
            encoding |= ((imm & 0b110) << 22);
            encoding |= ((imm & 1) << 20);
            break;

        case INS_OPTS_SCALABLE_S:
            assert(isValidUimm<2>(imm));
            encoding = 0x100000;     // set the bit at location 20
            encoding |= (imm << 22); // encode immediate at location 23:22
            break;

        case INS_OPTS_SCALABLE_D:
            assert(isValidUimm<1>(imm));
            encoding = 0x400000;     // set the bit at location 22
            encoding |= (imm << 23); // encode immediate at location 23
            break;

        default:
            assert(!"Invalid size for vector register");
            break;
    }

    return encoding;
}

/*****************************************************************************
 *
 *  Returns the encoding for the field 'tszh:tszl:imm3' at bit locations '23-22:20-19:18-16'.
 */

/*static*/ emitter::code_t emitter::insEncodeSveElemsizeWithShift_tszh_tszl_imm3(const insOpts opt,
                                                                                 ssize_t       imm,
                                                                                 bool          isRightShift)
{
    code_t encoding = 0;

    imm = insEncodeShiftImmediate(optGetSveElemsize(opt), isRightShift, imm);

    switch (opt)
    {
        case INS_OPTS_SCALABLE_B:
            imm = imm & 0b111;     // bits 18-16
            encoding |= (1 << 19); // bit 19
            break;

        case INS_OPTS_SCALABLE_H:
            imm = imm & 0b1111;    // bits 19-16
            encoding |= (1 << 20); // bit 20
            break;

        case INS_OPTS_SCALABLE_S:
            imm = imm & 0b11111;   // bits 20-16
            encoding |= (1 << 22); // bit 22
            break;

        case INS_OPTS_SCALABLE_D:
            // this gets the last bit of 'imm' and tries to set bit 22
            encoding |= ((imm >> 5) << 22);
            imm = imm & 0b11111;   // bits 20-16
            encoding |= (1 << 23); // bit 23
            break;

        default:
            assert(!"Invalid size for vector register");
            break;
    }

    return (encoding | (code_t)(imm << 16));
}

/*****************************************************************************
 *
 *  Returns the encoding for the field 'i1:tsz' at bit locations '20:19-16'.
 */

/*static*/ emitter::code_t emitter::insEncodeSveElemsizeWithImmediate_i1_tsz(const insOpts opt, ssize_t imm)
{
    code_t encoding = 0;

    switch (opt)
    {
        case INS_OPTS_SCALABLE_B:
            assert(isValidUimm<4>(imm));
            encoding |= (1 << 16);   // bit 16
            encoding |= (imm << 17); // bits 20-17
            break;

        case INS_OPTS_SCALABLE_H:
            assert(isValidUimm<3>(imm));
            encoding |= (1 << 17);   // bit 17
            encoding |= (imm << 18); // bits 20-18
            break;

        case INS_OPTS_SCALABLE_S:
            assert(isValidUimm<2>(imm));
            encoding |= (1 << 18);   // bit 18
            encoding |= (imm << 19); // bits 20-19
            break;

        case INS_OPTS_SCALABLE_D:
            assert(isValidUimm<1>(imm));
            encoding |= (1 << 19);   // bit 19
            encoding |= (imm << 20); // bit 20
            break;

        default:
            assert(!"Invalid size for vector register");
            break;
    }

    return encoding;
}

/*****************************************************************************
 *
 *  Returns the encoding to select the elemsize for an Arm64 SVE vector instruction plus an immediate.
 *  This specifically encodes the field 'tszh:tszl' at bit locations '23-22:9-8'.
 */

/*static*/ emitter::code_t emitter::insEncodeSveShift_23_to_22_9_to_0(emitAttr size, bool isRightShift, size_t imm)
{
    code_t encodedSize = 0;

    switch (size)
    {
        case EA_1BYTE:
            encodedSize = 0x100; // set the bit at location 8
            break;

        case EA_2BYTE:
            encodedSize = 0x200; // set the bit at location 9
            break;

        case EA_4BYTE:
            encodedSize = 0x400000; // set the bit at location 22
            break;

        case EA_8BYTE:
            encodedSize = 0x800000; // set the bit at location 23
            break;

        default:
            assert(!"Invalid esize for vector register");
    }

    code_t encodedImm = insEncodeShiftImmediate(size, isRightShift, imm);
    code_t imm3High   = (encodedImm & 0x60) << 17;
    code_t imm3Low    = (encodedImm & 0x1f) << 5;
    return encodedSize | imm3High | imm3Low;
}

/*****************************************************************************
 *
 *  Returns the encoding to select the constant values 90 or 270 for an Arm64 SVE vector instruction
 *  This specifically encode the field 'rot' at bit location '16'.
 */

/*static*/ emitter::code_t emitter::insEncodeSveImm90_or_270_rot(ssize_t imm)
{
    assert(emitIsValidEncodedRotationImm90_or_270(imm));
    return (code_t)(imm << 16);
}

/*****************************************************************************
 *
 *  Returns the encoding to select the constant values 0, 90, 180 or 270 for an Arm64 SVE vector instruction
 *  This specifically encode the field 'rot' at bit locations '14-13'.
 */

/*static*/ emitter::code_t emitter::insEncodeSveImm0_to_270_rot(ssize_t imm)
{
    assert(emitIsValidEncodedRotationImm0_to_270(imm));
    return (code_t)(imm << 13);
}

/*****************************************************************************
 *
 *  Returns the encoding to select the constant float values 0, 0.5, 1.0 or 2.0 for an Arm64 SVE vector instruction
 *  This specifically encode the field 'i1' at bit location '5'.
 */

/*static*/ emitter::code_t emitter::insEncodeSveSmallFloatImm(ssize_t imm)
{
    assert(emitIsValidEncodedSmallFloatImm(imm));
    return (code_t)(imm << 5);
}

/*****************************************************************************
 *
 *  Returns the register list size for the given SVE instruction.
 */

/*static*/ int emitter::insGetSveReg1ListSize(instruction ins)
{
    switch (ins)
    {
        case INS_sve_ld1d:
        case INS_sve_ld1w:
        case INS_sve_ld1sw:
        case INS_sve_ld1sb:
        case INS_sve_ld1b:
        case INS_sve_ld1sh:
        case INS_sve_ld1h:
        case INS_sve_ldnf1d:
        case INS_sve_ldnf1sw:
        case INS_sve_ldnf1sh:
        case INS_sve_ldnf1w:
        case INS_sve_ldnf1h:
        case INS_sve_ldnf1sb:
        case INS_sve_ldnf1b:
        case INS_sve_ldnt1b:
        case INS_sve_ldnt1d:
        case INS_sve_ldnt1h:
        case INS_sve_ldnt1w:
        case INS_sve_ld1rob:
        case INS_sve_ld1rod:
        case INS_sve_ld1roh:
        case INS_sve_ld1row:
        case INS_sve_ld1rqb:
        case INS_sve_ld1rqd:
        case INS_sve_ld1rqh:
        case INS_sve_ld1rqw:
        case INS_sve_stnt1b:
        case INS_sve_stnt1d:
        case INS_sve_stnt1h:
        case INS_sve_stnt1w:
        case INS_sve_st1d:
        case INS_sve_st1w:
        case INS_sve_ldff1sh:
        case INS_sve_ldff1w:
        case INS_sve_ldff1h:
        case INS_sve_ldff1d:
        case INS_sve_ldff1sw:
        case INS_sve_st1b:
        case INS_sve_st1h:
        case INS_sve_ldff1sb:
        case INS_sve_ldff1b:
        case INS_sve_ldnt1sb:
        case INS_sve_ldnt1sh:
        case INS_sve_ld1rd:
        case INS_sve_ld1rsw:
        case INS_sve_ld1rh:
        case INS_sve_ld1rsb:
        case INS_sve_ld1rsh:
        case INS_sve_ld1rw:
        case INS_sve_ld1q:
        case INS_sve_ldnt1sw:
        case INS_sve_st1q:
        case INS_sve_ld1rb:
            return 1;

        case INS_sve_ld2b:
        case INS_sve_ld2h:
        case INS_sve_ld2w:
        case INS_sve_ld2d:
        case INS_sve_ld2q:
        case INS_sve_splice: // SVE_CV_3A
        case INS_sve_st2b:
        case INS_sve_st2h:
        case INS_sve_st2w:
        case INS_sve_st2d:
        case INS_sve_st2q:
        case INS_sve_whilege: // SVE_DX_3A
        case INS_sve_whilegt: // SVE_DX_3A
        case INS_sve_whilehi: // SVE_DX_3A
        case INS_sve_whilehs: // SVE_DX_3A
        case INS_sve_whilele: // SVE_DX_3A
        case INS_sve_whilels: // SVE_DX_3A
        case INS_sve_whilelt: // SVE_DX_3A
        case INS_sve_pext:    // SVE_DW_2B
            return 2;

        case INS_sve_ld3b:
        case INS_sve_ld3h:
        case INS_sve_ld3w:
        case INS_sve_ld3d:
        case INS_sve_ld3q:
        case INS_sve_st3b:
        case INS_sve_st3h:
        case INS_sve_st3w:
        case INS_sve_st3d:
        case INS_sve_st3q:
            return 3;

        case INS_sve_ld4b:
        case INS_sve_ld4h:
        case INS_sve_ld4w:
        case INS_sve_ld4d:
        case INS_sve_ld4q:
        case INS_sve_st4b:
        case INS_sve_st4h:
        case INS_sve_st4w:
        case INS_sve_st4d:
        case INS_sve_st4q:
            return 4;

        default:
            assert(!"Unexpected instruction");
            return 1;
    }
}

/*****************************************************************************
 *
 *  Returns the predicate type for the given SVE format.
 */

/*static*/ emitter::PredicateType emitter::insGetPredicateType(insFormat fmt, int regpos /* =0 */)
{
    switch (fmt)
    {
        case IF_SVE_BV_2A:
        case IF_SVE_HW_4A:
        case IF_SVE_HW_4A_A:
        case IF_SVE_HW_4A_B:
        case IF_SVE_HW_4A_C:
        case IF_SVE_HW_4B:
        case IF_SVE_HW_4B_D:
        case IF_SVE_HX_3A_E:
        case IF_SVE_IJ_3A_D:
        case IF_SVE_IJ_3A_E:
        case IF_SVE_IJ_3A_F:
        case IF_SVE_IK_4A_G:
        case IF_SVE_IJ_3A_G:
        case IF_SVE_IK_4A_I:
        case IF_SVE_IH_3A_F:
        case IF_SVE_II_4A_H:
        case IF_SVE_IH_3A:
        case IF_SVE_IH_3A_A:
        case IF_SVE_II_4A:
        case IF_SVE_II_4A_B:
        case IF_SVE_IU_4A:
        case IF_SVE_IU_4A_C:
        case IF_SVE_IU_4B:
        case IF_SVE_IU_4B_D:
        case IF_SVE_IV_3A:
        case IF_SVE_IG_4A_F:
        case IF_SVE_IG_4A_G:
        case IF_SVE_IJ_3A:
        case IF_SVE_IK_4A:
        case IF_SVE_IK_4A_F:
        case IF_SVE_IK_4A_H:
        case IF_SVE_IU_4A_A:
        case IF_SVE_IU_4B_B:
        case IF_SVE_HX_3A_B:
        case IF_SVE_IG_4A:
        case IF_SVE_IG_4A_D:
        case IF_SVE_IG_4A_E:
        case IF_SVE_IF_4A:
        case IF_SVE_IF_4A_A:
        case IF_SVE_IM_3A:
        case IF_SVE_IN_4A:
        case IF_SVE_IX_4A:
        case IF_SVE_IO_3A:
        case IF_SVE_IP_4A:
        case IF_SVE_IQ_3A:
        case IF_SVE_IR_4A:
        case IF_SVE_IS_3A:
        case IF_SVE_IT_4A:
        case IF_SVE_GI_4A:
        case IF_SVE_IC_3A_C:
        case IF_SVE_IC_3A:
        case IF_SVE_IC_3A_B:
        case IF_SVE_IC_3A_A:
        case IF_SVE_IL_3A_C:
        case IF_SVE_IL_3A:
        case IF_SVE_IL_3A_B:
        case IF_SVE_IL_3A_A:
        case IF_SVE_IW_4A:
            return PREDICATE_ZERO;

        case IF_SVE_BV_2A_J:
        case IF_SVE_CP_3A:
        case IF_SVE_CQ_3A:
        case IF_SVE_AM_2A:
        case IF_SVE_AO_3A:
        case IF_SVE_HL_3A:
        case IF_SVE_HM_2A:
        case IF_SVE_AA_3A:
        case IF_SVE_BU_2A:
        case IF_SVE_BV_2B:
        case IF_SVE_HS_3A:
        case IF_SVE_HP_3A:
        case IF_SVE_HP_3B:
        case IF_SVE_AR_4A:
        case IF_SVE_BV_2A_A:
        case IF_SVE_HU_4A:
        case IF_SVE_HL_3B:
        case IF_SVE_AB_3B:
        case IF_SVE_GT_4A:
        case IF_SVE_AP_3A:
        case IF_SVE_HO_3A:
        case IF_SVE_HO_3B:
        case IF_SVE_HO_3C:
        case IF_SVE_GQ_3A:
        case IF_SVE_HU_4B:
        case IF_SVE_AQ_3A:
        case IF_SVE_CU_3A:
        case IF_SVE_AC_3A:
        case IF_SVE_GR_3A:
        case IF_SVE_ES_3A:
        case IF_SVE_HR_3A:
        case IF_SVE_GP_3A:
        case IF_SVE_EQ_3A:
        case IF_SVE_HQ_3A:
        case IF_SVE_AS_4A:
        case IF_SVE_CT_3A:
        case IF_SVE_HV_4A:
            return PREDICATE_MERGE;

        case IF_SVE_CZ_4A_A:
        case IF_SVE_CZ_4A_L:
        case IF_SVE_CE_2A:
        case IF_SVE_CE_2B:
        case IF_SVE_CE_2C:
        case IF_SVE_CE_2D:
        case IF_SVE_CF_2A:
        case IF_SVE_CF_2B:
        case IF_SVE_CF_2C:
        case IF_SVE_CF_2D:
        case IF_SVE_CI_3A:
        case IF_SVE_CJ_2A:
        case IF_SVE_DE_1A:
        case IF_SVE_DH_1A:
        case IF_SVE_DJ_1A:
        case IF_SVE_DM_2A:
        case IF_SVE_DN_2A:
        case IF_SVE_DO_2A:
        case IF_SVE_DP_2A:
        case IF_SVE_DR_1A:
        case IF_SVE_DT_3A:
        case IF_SVE_DU_3A:
        case IF_SVE_CK_2A:
            return PREDICATE_SIZED;

        case IF_SVE_DB_3A:
            // Second register could be ZERO or MERGE so handled at source.
            assert(regpos != 2);
            return PREDICATE_SIZED;

        case IF_SVE_DL_2A:
        case IF_SVE_DY_3A:
        case IF_SVE_DZ_1A:
            return PREDICATE_N_SIZED;

        // This is a special case as the second register could be ZERO or MERGE.
        // <Pg>/<ZM>
        // Therefore, by default return NONE due to ambiguity.
        case IF_SVE_AH_3A:
            // TODO: Handle these cases.
            assert(false);
            break;

        case IF_SVE_JD_4B:
        case IF_SVE_JD_4C:
        case IF_SVE_JI_3A_A:
        case IF_SVE_JJ_4A:
        case IF_SVE_JJ_4A_B:
        case IF_SVE_JJ_4A_C:
        case IF_SVE_JJ_4A_D:
        case IF_SVE_JJ_4B:
        case IF_SVE_JJ_4B_E:
        case IF_SVE_JN_3B:
        case IF_SVE_JN_3C:
        case IF_SVE_JD_4A:
        case IF_SVE_JN_3A:
        case IF_SVE_JD_4C_A:
        case IF_SVE_JJ_4B_C:
        case IF_SVE_JL_3A:
        case IF_SVE_JN_3C_D:
        case IF_SVE_HY_3A:
        case IF_SVE_HY_3A_A:
        case IF_SVE_HY_3B:
        case IF_SVE_HZ_2A_B:
        case IF_SVE_IA_2A:
        case IF_SVE_IB_3A:
        case IF_SVE_JK_4A:
        case IF_SVE_JK_4A_B:
        case IF_SVE_JK_4B:
        case IF_SVE_IZ_4A:
        case IF_SVE_IZ_4A_A:
        case IF_SVE_JB_4A:
        case IF_SVE_JM_3A:
        case IF_SVE_CM_3A:
        case IF_SVE_CN_3A:
        case IF_SVE_CO_3A:
        case IF_SVE_JA_4A:
        case IF_SVE_CR_3A:
        case IF_SVE_CS_3A:
        case IF_SVE_CV_3A:
        case IF_SVE_CV_3B:
        case IF_SVE_DW_2A: // <PNn>[<imm>]
        case IF_SVE_DW_2B: // <PNn>[<imm>]
        case IF_SVE_JC_4A:
        case IF_SVE_JO_3A:
        case IF_SVE_JE_3A:
        case IF_SVE_JF_4A:
        case IF_SVE_AK_3A:
        case IF_SVE_HE_3A:
        case IF_SVE_AF_3A:
        case IF_SVE_AG_3A:
        case IF_SVE_AI_3A:
        case IF_SVE_AJ_3A:
        case IF_SVE_AL_3A:
        case IF_SVE_CL_3A:
        case IF_SVE_GS_3A:
        case IF_SVE_HJ_3A:
        case IF_SVE_IY_4A:
            return PREDICATE_NONE;

        case IF_SVE_CX_4A:
        case IF_SVE_CX_4A_A:
        case IF_SVE_CY_3A:
        case IF_SVE_CY_3B:
        case IF_SVE_GE_4A:
        case IF_SVE_HT_4A:
            assert((regpos == 1) || (regpos == 2));
            return (regpos == 2 ? PREDICATE_ZERO : PREDICATE_SIZED);

        case IF_SVE_CZ_4A:
        case IF_SVE_DA_4A:
        case IF_SVE_DB_3B:
        case IF_SVE_DC_3A:
            assert((regpos >= 1) && (regpos <= 4));
            return (regpos == 2 ? PREDICATE_ZERO : PREDICATE_SIZED);

        case IF_SVE_CZ_4A_K:
            assert((regpos >= 1) && (regpos <= 3));
            return (regpos == 2 ? PREDICATE_MERGE : PREDICATE_SIZED);

        case IF_SVE_DD_2A:
        case IF_SVE_DF_2A:
            assert((regpos >= 1) && (regpos <= 3));
            return ((regpos == 2) ? PREDICATE_NONE : PREDICATE_SIZED);

        case IF_SVE_DG_2A:
            return (regpos == 2 ? PREDICATE_ZERO : PREDICATE_SIZED);

        case IF_SVE_DI_2A:
            return (regpos == 1 ? PREDICATE_NONE : PREDICATE_SIZED);

        case IF_SVE_DK_3A:
            assert((regpos == 2) || (regpos == 3));
            return ((regpos == 2) ? PREDICATE_NONE : PREDICATE_SIZED);

        case IF_SVE_HI_3A:
            assert((regpos == 1) || (regpos == 2));
            return ((regpos == 2) ? PREDICATE_ZERO : PREDICATE_SIZED);

        case IF_SVE_DV_4A:
            assert((regpos >= 1) && (regpos <= 3));
            return ((regpos == 3) ? PREDICATE_SIZED : PREDICATE_NONE);

        case IF_SVE_ID_2A:
        case IF_SVE_JG_2A:
            return PREDICATE_NONE;

        default:
            break;
    }

    assert(!"Unexpected instruction format");
    return PREDICATE_NONE;
}

/*****************************************************************************
 *
 *  Returns true if the SVE instruction has a LSL addr.
 *  This is for formats that have [<Xn|SP>, <Xm>, LSL #N], [<Xn|SP>{, <Xm>, LSL #N}]
 */
/*static*/ bool emitter::insSveIsLslN(instruction ins, insFormat fmt)
{
    switch (fmt)
    {
        case IF_SVE_JD_4A:
            switch (ins)
            {
                case INS_sve_st1h:
                    return true;

                default:
                    break;
            }
            break;

        case IF_SVE_JD_4B:
            switch (ins)
            {
                case INS_sve_st1w:
                    return true;

                default:
                    break;
            }
            break;

        case IF_SVE_HW_4B:
            switch (ins)
            {
                case INS_sve_ld1h:
                case INS_sve_ld1sh:
                case INS_sve_ldff1h:
                case INS_sve_ldff1sh:
                case INS_sve_ld1w:
                case INS_sve_ldff1w:
                    return true;

                default:
                    break;
            }
            break;

        case IF_SVE_IG_4A:
            switch (ins)
            {
                case INS_sve_ldff1d:
                case INS_sve_ldff1sw:
                    return true;

                default:
                    break;
            }
            break;

        case IF_SVE_IG_4A_F:
            switch (ins)
            {
                case INS_sve_ldff1sh:
                case INS_sve_ldff1w:
                    return true;

                default:
                    break;
            }
            break;

        case IF_SVE_IG_4A_G:
            switch (ins)
            {
                case INS_sve_ldff1h:
                    return true;

                default:
                    break;
            }
            break;

        case IF_SVE_II_4A:
        case IF_SVE_II_4A_B:
            switch (ins)
            {
                case INS_sve_ld1d:
                    return true;

                default:
                    break;
            }
            break;

        case IF_SVE_II_4A_H:
            switch (ins)
            {
                case INS_sve_ld1w:
                    return true;

                default:
                    break;
            }
            break;

        case IF_SVE_IK_4A:
            switch (ins)
            {
                case INS_sve_ld1sw:
                    return true;

                default:
                    break;
            }
            break;

        case IF_SVE_IK_4A_G:
            switch (ins)
            {
                case INS_sve_ld1sh:
                    return true;

                default:
                    break;
            }
            break;

        case IF_SVE_IK_4A_I:
            switch (ins)
            {
                case INS_sve_ld1h:
                    return true;

                default:
                    break;
            }
            break;

        case IF_SVE_IN_4A:
            switch (ins)
            {
                case INS_sve_ldnt1d:
                case INS_sve_ldnt1h:
                case INS_sve_ldnt1w:
                    return true;

                default:
                    break;
            }
            break;

        case IF_SVE_IP_4A:
            switch (ins)
            {
                case INS_sve_ld1roh:
                case INS_sve_ld1row:
                case INS_sve_ld1rod:
                case INS_sve_ld1rqh:
                case INS_sve_ld1rqw:
                case INS_sve_ld1rqd:
                    return true;

                default:
                    break;
            }
            break;

        case IF_SVE_IR_4A:
            switch (ins)
            {
                case INS_sve_ld2q:
                case INS_sve_ld3q:
                case INS_sve_ld4q:
                    return true;

                default:
                    break;
            }
            break;

        case IF_SVE_IT_4A:
            switch (ins)
            {
                case INS_sve_ld2h:
                case INS_sve_ld2w:
                case INS_sve_ld2d:
                case INS_sve_ld3h:
                case INS_sve_ld3w:
                case INS_sve_ld3d:
                case INS_sve_ld4h:
                case INS_sve_ld4w:
                case INS_sve_ld4d:
                    return true;

                default:
                    break;
            }
            break;

        case IF_SVE_IU_4B:
            switch (ins)
            {
                case INS_sve_ld1sw:
                case INS_sve_ldff1sw:
                case INS_sve_ld1d:
                case INS_sve_ldff1d:
                    return true;

                default:
                    break;
            }
            break;

        case IF_SVE_JB_4A:
            switch (ins)
            {
                case INS_sve_stnt1h:
                case INS_sve_stnt1w:
                case INS_sve_stnt1d:
                    return true;

                default:
                    break;
            }
            break;

        case IF_SVE_JC_4A:
            switch (ins)
            {
                case INS_sve_st2h:
                case INS_sve_st2w:
                case INS_sve_st2d:
                case INS_sve_st3h:
                case INS_sve_st3w:
                case INS_sve_st3d:
                case INS_sve_st4h:
                case INS_sve_st4w:
                case INS_sve_st4d:
                    return true;

                default:
                    break;
            }
            break;

        case IF_SVE_JD_4C:
            switch (ins)
            {
                case INS_sve_st1w:
                case INS_sve_st1d:
                    return true;

                default:
                    break;
            }
            break;

        case IF_SVE_JD_4C_A:
            switch (ins)
            {
                case INS_sve_st1d:
                    return true;

                default:
                    break;
            }
            break;

        case IF_SVE_JF_4A:
            switch (ins)
            {
                case INS_sve_st2q:
                case INS_sve_st3q:
                case INS_sve_st4q:
                    return true;

                default:
                    break;
            }
            break;

        case IF_SVE_JJ_4B:
            switch (ins)
            {
                case INS_sve_st1h:
                case INS_sve_st1w:
                case INS_sve_st1d:
                    return true;

                default:
                    break;
            }
            break;

        case IF_SVE_HY_3B:
        case IF_SVE_IB_3A:
            switch (ins)
            {
                case INS_sve_prfh:
                case INS_sve_prfw:
                case INS_sve_prfd:
                    return true;

                default:
                    break;
            }
            break;

        default:
            break;
    }

    return false;
}

/*****************************************************************************
 *
 *  Returns true if the SVE instruction has a <mod> addr.
 *  This is for formats that have [<Xn|SP>, <Zm>.T, <mod>], [<Xn|SP>, <Zm>.T, <mod> #N]
 */
/*static*/ bool emitter::insSveIsModN(instruction ins, insFormat fmt)
{
    switch (fmt)
    {
        case IF_SVE_JJ_4A:
        case IF_SVE_JJ_4A_B:
            switch (ins)
            {
                case INS_sve_st1d:
                case INS_sve_st1h:
                case INS_sve_st1w:
                    return true;

                default:
                    break;
            }
            break;

        case IF_SVE_JJ_4A_C:
        case IF_SVE_JJ_4A_D:
            switch (ins)
            {
                case INS_sve_st1h:
                case INS_sve_st1w:
                    return true;

                default:
                    break;
            }
            break;

        case IF_SVE_JK_4A:
        case IF_SVE_JK_4A_B:
            switch (ins)
            {
                case INS_sve_st1b:
                    return true;

                default:
                    break;
            }
            break;

        case IF_SVE_HW_4A:
        case IF_SVE_HW_4A_A:
            switch (ins)
            {
                case INS_sve_ld1b:
                case INS_sve_ld1h:
                case INS_sve_ld1sb:
                case INS_sve_ld1sh:
                case INS_sve_ld1w:
                case INS_sve_ldff1b:
                case INS_sve_ldff1h:
                case INS_sve_ldff1sb:
                case INS_sve_ldff1sh:
                case INS_sve_ldff1w:
                    return true;

                default:
                    break;
            }
            break;

        case IF_SVE_HW_4A_B:
        case IF_SVE_HW_4A_C:
            switch (ins)
            {
                case INS_sve_ld1h:
                case INS_sve_ld1sh:
                case INS_sve_ld1w:
                case INS_sve_ldff1h:
                case INS_sve_ldff1sh:
                case INS_sve_ldff1w:
                    return true;

                default:
                    break;
            }
            break;

        case IF_SVE_IU_4A:
            switch (ins)
            {
                case INS_sve_ld1d:
                case INS_sve_ld1sw:
                case INS_sve_ldff1d:
                case INS_sve_ldff1sw:
                    return true;

                default:
                    break;
            }
            break;

        case IF_SVE_IU_4A_A:
            switch (ins)
            {
                case INS_sve_ld1sw:
                case INS_sve_ldff1d:
                case INS_sve_ldff1sw:
                    return true;

                default:
                    break;
            }
            break;

        case IF_SVE_IU_4A_C:
            switch (ins)
            {
                case INS_sve_ld1d:
                    return true;

                default:
                    break;
            }
            break;

        case IF_SVE_HY_3A:
        case IF_SVE_HY_3A_A:
            switch (ins)
            {
                case INS_sve_prfb:
                case INS_sve_prfh:
                case INS_sve_prfw:
                case INS_sve_prfd:
                    return true;

                default:
                    break;
            }
            break;

        default:
            break;
    }

    return false;
}

/*****************************************************************************
 *
 *  Returns 0, 1, 2, 3 or 4 depending on the instruction and format.
 *  This is for formats that have [<Xn|SP>, <Zm>.T, <mod>], [<Xn|SP>, <Zm>.T, <mod> #N], [<Xn|SP>, <Xm>, LSL #N],
 * [<Xn|SP>{, <Xm>, LSL #N}]
 */

/*static*/ int emitter::insSveGetLslOrModN(instruction ins, insFormat fmt)
{
    switch (fmt)
    {
        case IF_SVE_JD_4A:
            assert(insSveIsLslN(ins, fmt));
            assert(!insSveIsModN(ins, fmt));
            switch (ins)
            {
                case INS_sve_st1h:
                    return 1;

                default:
                    break;
            }
            break;

        case IF_SVE_JD_4B:
            assert(insSveIsLslN(ins, fmt));
            assert(!insSveIsModN(ins, fmt));
            switch (ins)
            {
                case INS_sve_st1w:
                    return 2;

                default:
                    break;
            }
            break;

        case IF_SVE_HW_4B:
            assert(insSveIsLslN(ins, fmt));
            assert(!insSveIsModN(ins, fmt));
            switch (ins)
            {
                case INS_sve_ld1h:
                case INS_sve_ld1sh:
                case INS_sve_ldff1h:
                case INS_sve_ldff1sh:
                    return 1;

                case INS_sve_ld1w:
                case INS_sve_ldff1w:
                    return 2;

                default:
                    break;
            }
            break;

        case IF_SVE_JJ_4A:
        case IF_SVE_JJ_4A_B:
        case IF_SVE_JJ_4A_C:
        case IF_SVE_JJ_4A_D:
        case IF_SVE_JK_4A:
        case IF_SVE_JK_4A_B:
        case IF_SVE_HW_4A:
        case IF_SVE_HW_4A_A:
        case IF_SVE_HW_4A_B:
        case IF_SVE_HW_4A_C:
        case IF_SVE_IU_4A:
        case IF_SVE_IU_4A_A:
        case IF_SVE_IU_4A_C:
            assert(!insSveIsLslN(ins, fmt));
            assert(insSveIsModN(ins, fmt));
            switch (ins)
            {
                case INS_sve_ld1h:
                case INS_sve_ld1sh:
                case INS_sve_ldff1h:
                case INS_sve_ldff1sh:
                    switch (fmt)
                    {
                        case IF_SVE_HW_4A:
                        case IF_SVE_HW_4A_A:
                            return 1;

                        default:
                            break;
                    }
                    return 0;

                case INS_sve_ld1w:
                case INS_sve_ldff1w:
                case INS_sve_ld1sw:
                case INS_sve_ldff1sw:
                    switch (fmt)
                    {
                        case IF_SVE_HW_4A:
                        case IF_SVE_HW_4A_A:
                        case IF_SVE_IU_4A:
                            return 2;

                        default:
                            break;
                    }
                    return 0;

                case INS_sve_ld1d:
                case INS_sve_ldff1d:
                    switch (fmt)
                    {
                        case IF_SVE_IU_4A:
                            return 3;

                        default:
                            break;
                    }
                    return 0;

                case INS_sve_st1h:
                    switch (fmt)
                    {
                        case IF_SVE_JJ_4A_C:
                        case IF_SVE_JJ_4A_D:
                            return 0;

                        default:
                            break;
                    }
                    return 1;

                case INS_sve_st1w:
                    switch (fmt)
                    {
                        case IF_SVE_JJ_4A_C:
                        case IF_SVE_JJ_4A_D:
                            return 0;

                        default:
                            break;
                    }
                    return 2;

                case INS_sve_st1d:
                    if (fmt == IF_SVE_JJ_4A_B)
                    {
                        return 0;
                    }
                    return 3;

                default:
                    break;
            }
            return 0;

        case IF_SVE_IG_4A:
            assert(insSveIsLslN(ins, fmt));
            assert(!insSveIsModN(ins, fmt));
            switch (ins)
            {
                case INS_sve_ldff1sw:
                    return 2;

                case INS_sve_ldff1d:
                    return 3;

                default:
                    break;
            }
            break;

        case IF_SVE_IG_4A_F:
            assert(insSveIsLslN(ins, fmt));
            assert(!insSveIsModN(ins, fmt));
            switch (ins)
            {
                case INS_sve_ldff1sh:
                    return 1;

                case INS_sve_ldff1w:
                    return 2;

                default:
                    break;
            }
            break;

        case IF_SVE_IG_4A_G:
            assert(insSveIsLslN(ins, fmt));
            assert(!insSveIsModN(ins, fmt));
            switch (ins)
            {
                case INS_sve_ldff1h:
                    return 1;

                default:
                    break;
            }
            break;

        case IF_SVE_II_4A:
        case IF_SVE_II_4A_B:
            assert(insSveIsLslN(ins, fmt));
            assert(!insSveIsModN(ins, fmt));
            switch (ins)
            {
                case INS_sve_ld1d:
                    return 3;

                default:
                    break;
            }
            break;

        case IF_SVE_II_4A_H:
            assert(insSveIsLslN(ins, fmt));
            assert(!insSveIsModN(ins, fmt));
            switch (ins)
            {
                case INS_sve_ld1w:
                    return 2;

                default:
                    break;
            }
            break;

        case IF_SVE_IK_4A:
            assert(insSveIsLslN(ins, fmt));
            assert(!insSveIsModN(ins, fmt));
            switch (ins)
            {
                case INS_sve_ld1sw:
                    return 2;

                default:
                    break;
            }
            break;

        case IF_SVE_IK_4A_G:
            assert(insSveIsLslN(ins, fmt));
            assert(!insSveIsModN(ins, fmt));
            switch (ins)
            {
                case INS_sve_ld1sh:
                    return 1;

                default:
                    break;
            }
            break;

        case IF_SVE_IK_4A_I:
            assert(insSveIsLslN(ins, fmt));
            assert(!insSveIsModN(ins, fmt));
            switch (ins)
            {
                case INS_sve_ld1h:
                    return 1;

                default:
                    break;
            }
            break;

        case IF_SVE_IN_4A:
            assert(insSveIsLslN(ins, fmt));
            assert(!insSveIsModN(ins, fmt));
            switch (ins)
            {
                case INS_sve_ldnt1h:
                    return 1;
                case INS_sve_ldnt1w:
                    return 2;
                case INS_sve_ldnt1d:
                    return 3;

                default:
                    break;
            }
            break;

        case IF_SVE_IP_4A:
            assert(insSveIsLslN(ins, fmt));
            assert(!insSveIsModN(ins, fmt));
            switch (ins)
            {
                case INS_sve_ld1roh:
                case INS_sve_ld1rqh:
                    return 1;

                case INS_sve_ld1row:
                case INS_sve_ld1rqw:
                    return 2;
                case INS_sve_ld1rod:
                case INS_sve_ld1rqd:
                    return 3;

                default:
                    break;
            }
            break;

        case IF_SVE_IR_4A:
            assert(insSveIsLslN(ins, fmt));
            assert(!insSveIsModN(ins, fmt));
            switch (ins)
            {
                case INS_sve_ld2q:
                case INS_sve_ld3q:
                case INS_sve_ld4q:
                    return 4;

                default:
                    break;
            }
            break;

        case IF_SVE_IT_4A:
            assert(insSveIsLslN(ins, fmt));
            assert(!insSveIsModN(ins, fmt));
            switch (ins)
            {
                case INS_sve_ld2h:
                case INS_sve_ld3h:
                case INS_sve_ld4h:
                    return 1;

                case INS_sve_ld2w:
                case INS_sve_ld3w:
                case INS_sve_ld4w:
                    return 2;

                case INS_sve_ld2d:
                case INS_sve_ld3d:
                case INS_sve_ld4d:
                    return 3;

                default:
                    break;
            }
            break;

        case IF_SVE_IU_4B:
            assert(insSveIsLslN(ins, fmt));
            assert(!insSveIsModN(ins, fmt));
            switch (ins)
            {
                case INS_sve_ld1sw:
                case INS_sve_ldff1sw:
                    return 2;

                case INS_sve_ld1d:
                case INS_sve_ldff1d:
                    return 3;

                default:
                    break;
            }
            break;

        case IF_SVE_JB_4A:
            assert(insSveIsLslN(ins, fmt));
            assert(!insSveIsModN(ins, fmt));
            switch (ins)
            {
                case INS_sve_stnt1h:
                    return 1;

                case INS_sve_stnt1w:
                    return 2;

                case INS_sve_stnt1d:
                    return 3;

                default:
                    break;
            }
            break;

        case IF_SVE_JC_4A:
            assert(insSveIsLslN(ins, fmt));
            assert(!insSveIsModN(ins, fmt));
            switch (ins)
            {
                case INS_sve_st2h:
                case INS_sve_st3h:
                case INS_sve_st4h:
                    return 1;

                case INS_sve_st2w:
                case INS_sve_st3w:
                case INS_sve_st4w:
                    return 2;

                case INS_sve_st2d:
                case INS_sve_st3d:
                case INS_sve_st4d:
                    return 3;

                default:
                    break;
            }
            break;

        case IF_SVE_JD_4C:
            assert(insSveIsLslN(ins, fmt));
            assert(!insSveIsModN(ins, fmt));
            switch (ins)
            {
                case INS_sve_st1w:
                    return 2;

                case INS_sve_st1d:
                    return 3;

                default:
                    break;
            }
            break;

        case IF_SVE_JD_4C_A:
            assert(insSveIsLslN(ins, fmt));
            assert(!insSveIsModN(ins, fmt));
            switch (ins)
            {
                case INS_sve_st1d:
                    return 3;

                default:
                    break;
            }
            break;

        case IF_SVE_JF_4A:
            assert(insSveIsLslN(ins, fmt));
            assert(!insSveIsModN(ins, fmt));
            switch (ins)
            {
                case INS_sve_st2q:
                case INS_sve_st3q:
                case INS_sve_st4q:
                    return 4;

                default:
                    break;
            }
            break;

        case IF_SVE_JJ_4B:
            assert(insSveIsLslN(ins, fmt));
            assert(!insSveIsModN(ins, fmt));
            switch (ins)
            {
                case INS_sve_st1h:
                    return 1;

                case INS_sve_st1w:
                    return 2;

                case INS_sve_st1d:
                    return 3;

                default:
                    break;
            }
            break;

        case IF_SVE_HY_3A:
        case IF_SVE_HY_3A_A:
            assert(!insSveIsLslN(ins, fmt));
            assert(insSveIsModN(ins, fmt));
            switch (ins)
            {
                case INS_sve_prfb:
                    return 0;

                case INS_sve_prfh:
                    return 1;

                case INS_sve_prfw:
                    return 2;

                case INS_sve_prfd:
                    return 3;

                default:
                    break;
            }
            break;

        case IF_SVE_HY_3B:
        case IF_SVE_IB_3A:
            assert(insSveIsLslN(ins, fmt));
            assert(!insSveIsModN(ins, fmt));
            switch (ins)
            {
                case INS_sve_prfh:
                    return 1;

                case INS_sve_prfw:
                    return 2;

                case INS_sve_prfd:
                    return 3;

                default:
                    break;
            }
            break;

        default:
            break;
    }

    assert(!"Unexpected instruction format");
    return 0;
}

/*****************************************************************************
 *
 *  Returns true if the specified instruction can encode the 'dtype' field.
 */

/*static*/ bool emitter::canEncodeSveElemsize_dtype(instruction ins)
{
    switch (ins)
    {
        case INS_sve_ld1w:
        case INS_sve_ld1sb:
        case INS_sve_ld1b:
        case INS_sve_ld1sh:
        case INS_sve_ld1h:
        case INS_sve_ldnf1sh:
        case INS_sve_ldnf1w:
        case INS_sve_ldnf1h:
        case INS_sve_ldnf1sb:
        case INS_sve_ldnf1b:
        case INS_sve_ldff1b:
        case INS_sve_ldff1sb:
        case INS_sve_ldff1h:
        case INS_sve_ldff1sh:
        case INS_sve_ldff1w:
            return true;

        default:
            return false;
    }
}

/*****************************************************************************
 *
 *  Returns the encoding to select the 1/2/4/8 byte elemsize for an Arm64 Sve vector instruction
 *  for the 'dtype' field.
 */

/*static*/ emitter::code_t emitter::insEncodeSveElemsize_dtype(instruction ins, emitAttr size, code_t code)
{
    assert(canEncodeSveElemsize_dtype(ins));
    assert(ins != INS_sve_ld1w);
    switch (size)
    {
        case EA_1BYTE:
            switch (ins)
            {
                case INS_sve_ld1b:
                case INS_sve_ldnf1b:
                case INS_sve_ldff1b:
                    return code; // By default, the instruction already encodes 8-bit.

                default:
                    assert(!"Invalid instruction for encoding dtype.");
            }
            return code;

        case EA_2BYTE:
            switch (ins)
            {
                case INS_sve_ld1b:
                case INS_sve_ld1h:
                case INS_sve_ldnf1b:
                case INS_sve_ldnf1h:
                case INS_sve_ldff1b:
                case INS_sve_ldff1h:
                    return code | (1 << 21); // Set bit '21' to 1.

                case INS_sve_ld1sb:
                case INS_sve_ldnf1sb:
                case INS_sve_ldff1sb:
                    return code | (1 << 22); // Set bit '22' to 1.

                default:
                    assert(!"Invalid instruction for encoding dtype.");
            }
            return code;

        case EA_4BYTE:
            switch (ins)
            {
                case INS_sve_ldnf1w:
                case INS_sve_ldff1w:
                    return code; // By default, the instruction already encodes 32-bit.

                case INS_sve_ld1b:
                case INS_sve_ld1h:
                case INS_sve_ldnf1b:
                case INS_sve_ldnf1h:
                case INS_sve_ldff1b:
                case INS_sve_ldff1h:
                    return code | (1 << 22); // Set bit '22' to 1.

                case INS_sve_ld1sb:
                case INS_sve_ld1sh:
                case INS_sve_ldnf1sb:
                case INS_sve_ldnf1sh:
                case INS_sve_ldff1sb:
                case INS_sve_ldff1sh:
                    return code | (1 << 21); // Set bit '21' to 1.

                default:
                    assert(!"Invalid instruction for encoding dtype.");
            }
            return code;

        case EA_8BYTE:
            switch (ins)
            {
                case INS_sve_ldnf1w:
                case INS_sve_ldff1w:
                    return code | (1 << 21); // Set bit '21' to 1. Set bit '15' to 1.

                case INS_sve_ld1b:
                case INS_sve_ld1h:
                case INS_sve_ldnf1b:
                case INS_sve_ldnf1h:
                case INS_sve_ldff1b:
                case INS_sve_ldff1h:
                    return (code | (1 << 22)) | (1 << 21); // Set bit '22' and '21' to 1.

                case INS_sve_ld1sb:
                case INS_sve_ld1sh:
                case INS_sve_ldnf1sb:
                case INS_sve_ldnf1sh:
                case INS_sve_ldff1sb:
                case INS_sve_ldff1sh:
                    return code; // By default, the instruction already encodes 64-bit.

                default:
                    assert(!"Invalid instruction for encoding dtype.");
            }
            return code;

        default:
            assert(!"Invalid size for encoding dtype.");
    }

    return code;
}

/*****************************************************************************
 *
 * Returns the encoding to select the 4/8/16 byte elemsize for the Arm64 Sve vector instruction 'ld1w'
 * for the 'dtype' field.
 */

/*static*/ emitter::code_t emitter::insEncodeSveElemsize_dtype_ld1w(instruction ins,
                                                                    insFormat   fmt,
                                                                    emitAttr    size,
                                                                    code_t      code)
{
    assert(canEncodeSveElemsize_dtype(ins));
    assert(ins == INS_sve_ld1w);
    switch (size)
    {
        case EA_4BYTE:
            switch (fmt)
            {
                case IF_SVE_IH_3A_F:
                    // Note: Bit '15' is not actually part of 'dtype', but it is necessary to set to '1' to get the
                    // proper encoding for S.
                    return (code | (1 << 15)) | (1 << 22); // Set bit '22' and '15' to 1.

                case IF_SVE_II_4A_H:
                    // Note: Bit '14' is not actually part of 'dtype', but it is necessary to set to '1' to get the
                    // proper encoding for S.
                    return (code | (1 << 14)) | (1 << 22); // Set bit '22' and '14' to 1.

                default:
                    break;
            }
            break;

        case EA_8BYTE:
            switch (fmt)
            {
                case IF_SVE_IH_3A_F:
                    // Note: Bit '15' is not actually part of 'dtype', but it is necessary to set to '1' to get the
                    // proper encoding for D.
                    return ((code | (1 << 15)) | (1 << 22)) | (1 << 21); // Set bit '22', '21' and '15' to 1.

                case IF_SVE_II_4A_H:
                    // Note: Bit '14' is not actually part of 'dtype', but it is necessary to set to '1' to get the
                    // proper encoding for D.
                    return ((code | (1 << 14)) | (1 << 22)) | (1 << 21); // Set bit '22', '21' and '14' to 1.

                default:
                    break;
            }
            break;

        case EA_16BYTE:
            switch (fmt)
            {
                case IF_SVE_IH_3A_F:
                    return code | (1 << 20); // Set bit '20' to 1.

                case IF_SVE_II_4A_H:
                    // Note: Bit '15' is not actually part of 'dtype', but it is necessary to set to '1' to get the
                    // proper encoding for Q.
                    return code | (1 << 15); // Set bit '15' to 1.

                default:
                    break;
            }
            break;

        default:
            assert(!"Invalid size for encoding dtype.");
            break;
    }

    assert(!"Invalid instruction format");
    return code;
}

/*****************************************************************************
 *
 *  Returns the encoding to select the 1/2/4/8 byte elemsize for an Arm64 Sve vector instruction
 *  for the 'dtypeh' and 'dtypel' fields.
 */

/*static*/ emitter::code_t emitter::insEncodeSveElemsize_dtypeh_dtypel(instruction ins,
                                                                       insFormat   fmt,
                                                                       emitAttr    size,
                                                                       code_t      code)
{
    switch (fmt)
    {
        case IF_SVE_IC_3A_A:
            switch (size)
            {
                case EA_4BYTE:
                    switch (ins)
                    {
                        case INS_sve_ld1rsh:
                            return code | (1 << 13); // set bit '13'

                        case INS_sve_ld1rw:
                            return code | (1 << 14); // set bit '14'

                        default:
                            break;
                    }
                    break;

                case EA_8BYTE:
                    switch (ins)
                    {
                        case INS_sve_ld1rsh:
                            return code;

                        case INS_sve_ld1rw:
                            return code | (1 << 14) | (1 << 13); // set bits '14' and '13'

                        default:
                            break;
                    }
                    break;

                default:
                    break;
            }
            break;

        case IF_SVE_IC_3A_B:
            switch (size)
            {
                case EA_2BYTE:
                    switch (ins)
                    {
                        case INS_sve_ld1rh:
                            return code | (1 << 13); // set bit '13'

                        case INS_sve_ld1rsb:
                            return code | (1 << 24) | (1 << 14); // set bit '24' and '14'

                        default:
                            break;
                    }
                    break;

                case EA_4BYTE:
                    switch (ins)
                    {
                        case INS_sve_ld1rh:
                            return code | (1 << 14); // set bit '14'

                        case INS_sve_ld1rsb:
                            return code | (1 << 24) | (1 << 13); // set bit '24' and '13'

                        default:
                            break;
                    }
                    break;

                case EA_8BYTE:
                    switch (ins)
                    {
                        case INS_sve_ld1rh:
                            return code | (1 << 14) | (1 << 13); // set bits '14' and '13'

                        case INS_sve_ld1rsb:
                            return code | (1 << 24); // set bit '24'

                        default:
                            break;
                    }
                    break;

                default:
                    break;
            }
            break;

        case IF_SVE_IC_3A_C:
            assert(ins == INS_sve_ld1rb);
            switch (size)
            {
                case EA_1BYTE:
                    return code;

                case EA_2BYTE:
                    return code | (1 << 13); // set bit '13'

                case EA_4BYTE:
                    return code | (1 << 14); // set bit '14'

                case EA_8BYTE:
                    return code | (1 << 14) | (1 << 13); // set bits '14' and '13'

                default:
                    break;
            }
            break;

        default:
            break;
    }

    assert(!"Unexpected instruction format");
    return code;
}

/*****************************************************************************
 *
 *  Returns the encoding to select the <R> 4/8-byte width specifier <R>
 *  at bit location 22 for an Arm64 Sve instruction.
 */
/*static*/ emitter::code_t emitter::insEncodeSveElemsize_R_22(emitAttr size)
{
    if (size == EA_8BYTE)
    {
        return 0x400000; // set the bit at location 22
    }

    assert(size == EA_4BYTE);
    return 0;
}

/*****************************************************************************
 *
 *  Returns the immediate value for SVE instructions that encode it as a difference
 *  from tszh:tszl:imm3.
 */
/*static*/ ssize_t emitter::insSveGetImmDiff(const ssize_t imm, const insOpts opt)
{
    switch (opt)
    {
        case INS_OPTS_SCALABLE_B:
            assert(isValidUimmFrom1<3>(imm));
            return (8 - imm);

        case INS_OPTS_SCALABLE_H:
            assert(isValidUimmFrom1<4>(imm));
            return (16 - imm);

        case INS_OPTS_SCALABLE_S:
            assert(isValidUimmFrom1<5>(imm));
            return (32 - imm);

        case INS_OPTS_SCALABLE_D:
            assert(isValidUimmFrom1<6>(imm));
            return (64 - imm);

        default:
            unreached();
            break;
    }

    return 0;
}

/*****************************************************************************
 *
 *  Returns the two 5-bit signed immediates encoded in the following format:
 *  njjj jjmi iiii
 *  - iiiii: the absolute value of imm1
 *  - m: 1 if imm1 is negative, 0 otherwise
 *  - jjjjj: the absolute value of imm2
 *  - n: 1 if imm2 is negative, 0 otherwise
 */
/*static*/ ssize_t emitter::insSveEncodeTwoSimm5(ssize_t imm1, ssize_t imm2)
{
    assert(isValidSimm<5>(imm1));
    assert(isValidSimm<5>(imm2));
    ssize_t immOut = 0;

    if (imm1 < 0)
    {
        // Set bit location 5 to indicate imm1 is negative
        immOut |= 0x20;
        imm1 *= -1;
    }

    if (imm2 < 0)
    {
        // Set bit location 11 to indicate imm2 is negative
        immOut |= 0x800;
        imm2 *= -1;
    }

    immOut |= imm1;
    immOut |= (imm2 << 6);
    return immOut;
}

/*****************************************************************************
 *
 *  Decodes imm into two 5-bit signed immediates,
 *  using the encoding format from insSveEncodeTwoSimm5.
 */
/*static*/ void emitter::insSveDecodeTwoSimm5(ssize_t imm, /* OUT */ ssize_t* const imm1, /* OUT */ ssize_t* const imm2)
{
    assert(imm1 != nullptr);
    assert(imm2 != nullptr);

    *imm1 = (imm & 0x1F);

    if ((imm & 0x20) != 0)
    {
        *imm1 *= -1;
    }

    imm >>= 6;
    *imm2 = (imm & 0x1F);

    if ((imm & 0x20) != 0)
    {
        *imm2 *= -1;
    }

    assert(isValidSimm<5>(*imm1));
    assert(isValidSimm<5>(*imm2));
}

/************************************************************************
 *
 *  Convert a small immediate float value to an encoded version that matches one-to-one with the instructions.
 *  The instruction determines the value.
 */

/*static*/ ssize_t emitter::emitEncodeSmallFloatImm(double immDbl, instruction ins)
{
#ifdef DEBUG
    switch (ins)
    {
        case INS_sve_fadd:
        case INS_sve_fsub:
        case INS_sve_fsubr:
            assert((immDbl == 0.5) || (immDbl == 1.0));
            break;

        case INS_sve_fmax:
        case INS_sve_fmaxnm:
        case INS_sve_fmin:
        case INS_sve_fminnm:
            assert((immDbl == 0) || (immDbl == 1.0));
            break;

        case INS_sve_fmul:
            assert((immDbl == 0.5) || (immDbl == 2.0));
            break;

        default:
            assert(!"Invalid instruction");
            break;
    }
#endif // DEBUG
    if (immDbl < 1.0)
    {
        return 0;
    }
    return 1;
}

/************************************************************************
 *
 *  Convert an encoded small float immediate value. The instruction determines the value.
 */

/*static*/ double emitter::emitDecodeSmallFloatImm(ssize_t imm, instruction ins)
{
    assert(emitIsValidEncodedSmallFloatImm(imm));
    switch (ins)
    {
        case INS_sve_fadd:
        case INS_sve_fsub:
        case INS_sve_fsubr:
            if (imm == 0)
            {
                return 0.5;
            }
            else
            {
                return 1.0;
            }

        case INS_sve_fmax:
        case INS_sve_fmaxnm:
        case INS_sve_fmin:
        case INS_sve_fminnm:
            if (imm == 0)
            {
                return 0.0;
            }
            else
            {
                return 1.0;
            }
            break;

        case INS_sve_fmul:
            if (imm == 0)
            {
                return 0.5;
            }
            else
            {
                return 2.0;
            }
            break;

        default:
            break;
    }

    assert(!"Invalid instruction");
    return 0.0;
}

/************************************************************************
 *
 *  Check if the immediate value is a valid encoded small float.
 */

/*static*/ bool emitter::emitIsValidEncodedSmallFloatImm(size_t imm)
{
    return (imm == 0) || (imm == 1);
}

/************************************************************************
 *
 *  Convert a rotation value that is 90 or 270 into a smaller encoding that matches one-to-one with the 'rot' field.
 */

/*static*/ ssize_t emitter::emitEncodeRotationImm90_or_270(ssize_t imm)
{
    switch (imm)
    {
        case 90:
            return 0;

        case 270:
            return 1;

        default:
            break;
    }

    assert(!"Invalid rotation value");
    return 0;
}

/************************************************************************
 *
 *  Convert an encoded rotation value to 90 or 270.
 */

/*static*/ ssize_t emitter::emitDecodeRotationImm90_or_270(ssize_t imm)
{
    assert(emitIsValidEncodedRotationImm0_to_270(imm));
    switch (imm)
    {
        case 0:
            return 90;

        case 1:
            return 270;

        default:
            break;
    }

    return 0;
}

/************************************************************************
 *
 *  Check if the immediate value is a valid encoded rotation value for 90 or 270.
 */

/*static*/ bool emitter::emitIsValidEncodedRotationImm90_or_270(ssize_t imm)
{
    return isValidUimm<1>(imm);
}

/************************************************************************
 *
 *  Convert a rotation value that is 0, 90, 180 or 270 into a smaller encoding that matches one-to-one with the 'rot'
 * field.
 */

/*static*/ ssize_t emitter::emitEncodeRotationImm0_to_270(ssize_t imm)
{
    switch (imm)
    {
        case 0:
            return 0;

        case 90:
            return 1;

        case 180:
            return 2;

        case 270:
            return 3;

        default:
            break;
    }

    assert(!"Invalid rotation value");
    return 0;
}

/************************************************************************
 *
 *  Convert an encoded rotation value to 0, 90, 180 or 270.
 */

/*static*/ ssize_t emitter::emitDecodeRotationImm0_to_270(ssize_t imm)
{
    assert(emitIsValidEncodedRotationImm0_to_270(imm));
    switch (imm)
    {
        case 0:
            return 0;

        case 1:
            return 90;

        case 2:
            return 180;

        case 3:
            return 270;

        default:
            break;
    }

    return 0;
}

/************************************************************************
 *
 *  Check if the immediate value is a valid encoded rotation value for 0, 90, 180 or 270.
 */

/*static*/ bool emitter::emitIsValidEncodedRotationImm0_to_270(ssize_t imm)
{
    return isValidUimm<2>(imm);
}

/*****************************************************************************
 *
 * Returns the encoding to select an insSvePattern
 */
/*static*/ emitter::code_t emitter::insEncodeSvePattern(insSvePattern pattern)
{
    return (code_t)((unsigned)pattern << 5);
}

/*****************************************************************************
 *
 *  Returns the encoding for an immediate in the SVE variant of dup (indexed)
 */
/*static*/ emitter::code_t emitter::insEncodeSveBroadcastIndex(emitAttr elemsize, ssize_t index)
{
    unsigned lane_bytes = genLog2(elemsize) + 1;
    code_t   tsz        = (1 << (lane_bytes - 1));
    code_t   imm        = (code_t)index << lane_bytes | tsz;
    return insEncodeSplitUimm<23, 22, 20, 16>(imm);
}

/*****************************************************************************
 *
 *  Append the machine code corresponding to the given SVE instruction descriptor.
 */
BYTE* emitter::emitOutput_InstrSve(BYTE* dst, instrDesc* id)
{
    code_t      code = 0;
    instruction ins  = id->idIns();
    insFormat   fmt  = id->idInsFmt();
    emitAttr    size = id->idOpSize();

    ssize_t imm;

    switch (fmt)
    {
        // Scalable.
        case IF_SVE_AA_3A: // ........xx...... ...gggmmmmmddddd
        case IF_SVE_AC_3A: // ........xx...... ...gggmmmmmddddd -- SVE integer divide vectors (predicated)
        case IF_SVE_AF_3A: // ........xx...... ...gggnnnnnddddd -- SVE bitwise logical reduction (predicated)
        case IF_SVE_AG_3A: // ........xx...... ...gggnnnnnddddd -- SVE bitwise logical reduction (quadwords)
        case IF_SVE_AI_3A: // ........xx...... ...gggnnnnnddddd -- SVE integer add reduction (predicated)
        case IF_SVE_AJ_3A: // ........xx...... ...gggnnnnnddddd -- SVE integer add reduction (quadwords)
        case IF_SVE_AK_3A: // ........xx...... ...gggnnnnnddddd -- SVE integer min/max reduction (predicated)
        case IF_SVE_AL_3A: // ........xx...... ...gggnnnnnddddd -- SVE integer min/max reduction (quadwords)
        case IF_SVE_AO_3A: // ........xx...... ...gggmmmmmddddd -- SVE bitwise shift by wide elements (predicated)
        case IF_SVE_AP_3A: // ........xx...... ...gggnnnnnddddd -- SVE bitwise unary operations (predicated)
        case IF_SVE_AQ_3A: // ........xx...... ...gggnnnnnddddd -- SVE integer unary operations (predicated)
        case IF_SVE_CL_3A: // ........xx...... ...gggnnnnnddddd -- SVE compress active elements
        case IF_SVE_CM_3A: // ........xx...... ...gggmmmmmddddd -- SVE conditionally broadcast element to vector
        case IF_SVE_CN_3A: // ........xx...... ...gggmmmmmddddd -- SVE conditionally extract element to SIMD&FP scalar
        case IF_SVE_CP_3A: // ........xx...... ...gggnnnnnddddd -- SVE copy SIMD&FP scalar register to vector
                           // (predicated)
        case IF_SVE_CR_3A: // ........xx...... ...gggnnnnnddddd -- SVE extract element to SIMD&FP scalar register
        case IF_SVE_CU_3A: // ........xx...... ...gggnnnnnddddd -- SVE reverse within elements
        case IF_SVE_EQ_3A: // ........xx...... ...gggnnnnnddddd -- SVE2 integer pairwise add and accumulate long
        case IF_SVE_ES_3A: // ........xx...... ...gggnnnnnddddd -- SVE2 integer unary operations (predicated)
        case IF_SVE_GR_3A: // ........xx...... ...gggmmmmmddddd -- SVE2 floating-point pairwise operations
        case IF_SVE_GS_3A: // ........xx...... ...gggnnnnnddddd -- SVE floating-point recursive reduction (quadwords)
        case IF_SVE_HE_3A: // ........xx...... ...gggnnnnnddddd -- SVE floating-point recursive reduction
        case IF_SVE_HJ_3A: // ........xx...... ...gggmmmmmddddd -- SVE floating-point serial reduction (predicated)
        case IF_SVE_HL_3A: // ........xx...... ...gggmmmmmddddd -- SVE floating-point arithmetic (predicated)
        case IF_SVE_HQ_3A: // ........xx...... ...gggnnnnnddddd -- SVE floating-point round to integral value
        case IF_SVE_HR_3A: // ........xx...... ...gggnnnnnddddd -- SVE floating-point unary operations
            code = emitInsCodeSve(ins, fmt);
            code |= insEncodeReg_V<4, 0>(id->idReg1());                      // ddddd
            code |= insEncodeReg_P<12, 10>(id->idReg2());                    // ggg
            code |= insEncodeReg_V<9, 5>(id->idReg3());                      // mmmmm or nnnnn
            code |= insEncodeSveElemsize(optGetSveElemsize(id->idInsOpt())); // xx
            dst += emitOutput_Instr(dst, code);
            break;

        case IF_SVE_AB_3B: // ................ ...gggmmmmmddddd -- SVE integer add/subtract vectors (predicated)
        case IF_SVE_HL_3B: // ................ ...gggmmmmmddddd -- SVE floating-point arithmetic (predicated)
            code = emitInsCodeSve(ins, fmt);
            code |= insEncodeReg_V<4, 0>(id->idReg1());   // ddddd
            code |= insEncodeReg_P<12, 10>(id->idReg2()); // ggg
            code |= insEncodeReg_V<9, 5>(id->idReg3());   // mmmmm
            dst += emitOutput_Instr(dst, code);
            break;

        // Scalable with Merge or Zero predicate
        case IF_SVE_AH_3A: // ........xx.....M ...gggnnnnnddddd -- SVE constructive prefix (predicated)
            code = emitInsCodeSve(ins, fmt);
            code |= insEncodeReg_V<4, 0>(id->idReg1());                      // nnnnn
            code |= insEncodeReg_P<12, 10>(id->idReg2());                    // ggg
            code |= insEncodeReg_V<9, 5>(id->idReg3());                      // ddddd
            code |= insEncodePredQualifier_16(id->idPredicateReg2Merge());   // M
            code |= insEncodeSveElemsize(optGetSveElemsize(id->idInsOpt())); // xx
            dst += emitOutput_Instr(dst, code);
            break;

        // Scalable with shift immediate
        case IF_SVE_AM_2A: // ........xx...... ...gggxxiiiddddd -- SVE bitwise shift by immediate (predicated)
        {
            bool isRightShift = emitInsIsVectorRightShift(ins);
            imm               = emitGetInsSC(id);
            code              = emitInsCodeSve(ins, fmt);
            code |= insEncodeReg_V<4, 0>(id->idReg1());   // ddddd
            code |= insEncodeReg_P<12, 10>(id->idReg2()); // ggg
            code |=
                insEncodeSveShift_23_to_22_9_to_0(optGetSveElemsize(id->idInsOpt()), isRightShift, imm); // xx, xxiii
            dst += emitOutput_Instr(dst, code);
        }
        break;

        // Scalable, 4 regs. Reg4 in mmmmm.
        case IF_SVE_AR_4A: // ........xx.mmmmm ...gggnnnnnddddd -- SVE integer multiply-accumulate writing addend
                           // (predicated)
        case IF_SVE_GI_4A: // ........xx.mmmmm ...gggnnnnnddddd -- SVE2 histogram generation (vector)
        case IF_SVE_HU_4A: // ........xx.mmmmm ...gggnnnnnddddd -- SVE floating-point multiply-accumulate writing addend
            code = emitInsCodeSve(ins, fmt);
            code |= insEncodeReg_V<4, 0>(id->idReg1());                      // ddddd
            code |= insEncodeReg_P<12, 10>(id->idReg2());                    // ggg
            code |= insEncodeReg_V<9, 5>(id->idReg3());                      // nnnnn
            code |= insEncodeReg_V<20, 16>(id->idReg4());                    // mmmmm
            code |= insEncodeSveElemsize(optGetSveElemsize(id->idInsOpt())); // xx
            dst += emitOutput_Instr(dst, code);
            break;

        // Scalable, 4 regs. Reg4 in aaaaa.
        case IF_SVE_AS_4A: // ........xx.mmmmm ...gggaaaaaddddd -- SVE integer multiply-add writing multiplicand
                           // (predicated)
            code = emitInsCodeSve(ins, fmt);
            code |= insEncodeReg_V<4, 0>(id->idReg1());                      // ddddd
            code |= insEncodeReg_P<12, 10>(id->idReg2());                    // ggg
            code |= insEncodeReg_V<20, 16>(id->idReg3());                    // mmmmm
            code |= insEncodeReg_V<9, 5>(id->idReg4());                      // aaaaa
            code |= insEncodeSveElemsize(optGetSveElemsize(id->idInsOpt())); // xx
            dst += emitOutput_Instr(dst, code);
            break;

        // Scalable, 3 regs, no predicates
        case IF_SVE_AT_3A:   // ........xx.mmmmm ......nnnnnddddd
        case IF_SVE_BG_3A:   // ........xx.mmmmm ......nnnnnddddd -- SVE bitwise shift by wide elements (unpredicated)
        case IF_SVE_BZ_3A:   // ........xx.mmmmm ......nnnnnddddd -- SVE table lookup (three sources)
        case IF_SVE_BZ_3A_A: // ........xx.mmmmm ......nnnnnddddd -- SVE table lookup (three sources)
        case IF_SVE_EH_3A:   // ........xx.mmmmm ......nnnnnddddd -- SVE integer dot product (unpredicated)
        case IF_SVE_EL_3A:   // ........xx.mmmmm ......nnnnnddddd
        case IF_SVE_EM_3A:   // ........xx.mmmmm ......nnnnnddddd -- SVE2 saturating multiply-add high
        case IF_SVE_EX_3A:   // ........xx.mmmmm ......nnnnnddddd -- SVE permute vector elements (quadwords)
        case IF_SVE_FL_3A:   // ........xx.mmmmm ......nnnnnddddd
        case IF_SVE_FM_3A:   // ........xx.mmmmm ......nnnnnddddd -- SVE2 integer add/subtract wide
        case IF_SVE_FW_3A:   // ........xx.mmmmm ......nnnnnddddd -- SVE2 integer absolute difference and accumulate
        case IF_SVE_GC_3A:   // ........xx.mmmmm ......nnnnnddddd -- SVE2 integer add/subtract narrow high part
        case IF_SVE_GF_3A:   // ........xx.mmmmm ......nnnnnddddd -- SVE2 histogram generation (segment)
            code = emitInsCodeSve(ins, fmt);
            code |= insEncodeReg_V<4, 0>(id->idReg1());                      // ddddd
            code |= insEncodeReg_V<9, 5>(id->idReg2());                      // nnnnn
            code |= insEncodeReg_V<20, 16>(id->idReg3());                    // mmmmm
            code |= insEncodeSveElemsize(optGetSveElemsize(id->idInsOpt())); // xx
            dst += emitOutput_Instr(dst, code);
            break;

        // Scalable, 3 regs, no predicates. General purpose source registers
        case IF_SVE_BA_3A: // ........xx.mmmmm ......nnnnnddddd -- SVE index generation (register start, register
                           // increment)
            code = emitInsCodeSve(ins, fmt);
            code |= insEncodeReg_V<4, 0>(id->idReg1());                      // ddddd
            code |= insEncodeReg_Rn(id->idReg2());                           // nnnnn
            code |= insEncodeReg_Rm(id->idReg3());                           // mmmmm
            code |= insEncodeSveElemsize(optGetSveElemsize(id->idInsOpt())); // xx
            dst += emitOutput_Instr(dst, code);
            break;

        case IF_SVE_BH_3A: // .........x.mmmmm ....hhnnnnnddddd -- SVE address generation
            code = emitInsCodeSve(ins, fmt);
            code |= insEncodeReg_V<4, 0>(id->idReg1());      // ddddd
            code |= insEncodeReg_V<9, 5>(id->idReg2());      // nnnnn
            code |= insEncodeReg_V<20, 16>(id->idReg3());    // mmmmm
            code |= insEncodeUimm<11, 10>(emitGetInsSC(id)); // hh
            code |= insEncodeUimm<22, 22>(id->idInsOpt() == INS_OPTS_SCALABLE_D ? 1 : 0);
            dst += emitOutput_Instr(dst, code);
            break;

        case IF_SVE_BH_3B:   // ...........mmmmm ....hhnnnnnddddd -- SVE address generation
        case IF_SVE_BH_3B_A: // ...........mmmmm ....hhnnnnnddddd -- SVE address generation
            code = emitInsCodeSve(ins, fmt);
            code |= insEncodeReg_V<4, 0>(id->idReg1());      // ddddd
            code |= insEncodeReg_V<9, 5>(id->idReg2());      // nnnnn
            code |= insEncodeReg_V<20, 16>(id->idReg3());    // mmmmm
            code |= insEncodeUimm<11, 10>(emitGetInsSC(id)); // hh
            dst += emitOutput_Instr(dst, code);
            break;

        // Immediate and pattern to general purpose.
        case IF_SVE_BL_1A: // ............iiii ......pppppddddd -- SVE element count
        case IF_SVE_BM_1A: // ............iiii ......pppppddddd -- SVE inc/dec register by element count
            imm  = emitGetInsSC(id);
            code = emitInsCodeSve(ins, fmt);
            code |= insEncodeReg_Rd(id->idReg1());           // ddddd
            code |= insEncodeSvePattern(id->idSvePattern()); // ppppp
            code |= insEncodeUimm<19, 16>(imm - 1);          // iiii
            dst += emitOutput_Instr(dst, code);
            break;

        case IF_SVE_BO_1A: // ...........Xiiii ......pppppddddd -- SVE saturating inc/dec register by element count
            imm  = emitGetInsSC(id);
            code = emitInsCodeSve(ins, fmt);
            code |= insEncodeReg_Rd(id->idReg1());              // ddddd
            code |= insEncodeSvePattern(id->idSvePattern());    // ppppp
            code |= insEncodeUimm<19, 16>(imm - 1);             // iiii
            code |= insEncodeSveElemsize_sz_20(id->idOpSize()); // X
            dst += emitOutput_Instr(dst, code);
            break;

        case IF_SVE_BQ_2A: // ...........iiiii ...iiinnnnnddddd -- SVE extract vector (immediate offset, destructive)
        case IF_SVE_BQ_2B: // ...........iiiii ...iiimmmmmddddd -- SVE extract vector (immediate offset, destructive)
            imm  = emitGetInsSC(id);
            code = emitInsCodeSve(ins, fmt);
            code |= insEncodeReg_V<4, 0>(id->idReg1()); // ddddd
            code |= insEncodeReg_V<9, 5>(id->idReg2()); // nnnnn/mmmmm
            code |= insEncodeUimm<12, 10>(imm & 0b111); // iii
            code |= insEncodeUimm<20, 16>(imm >> 3);    // iiiii
            dst += emitOutput_Instr(dst, code);
            break;

        case IF_SVE_BN_1A: // ............iiii ......pppppddddd -- SVE inc/dec vector by element count
        case IF_SVE_BP_1A: // ............iiii ......pppppddddd -- SVE saturating inc/dec vector by element count
            imm  = emitGetInsSC(id);
            code = emitInsCodeSve(ins, fmt);
            code |= insEncodeReg_V<4, 0>(id->idReg1());      // ddddd
            code |= insEncodeSvePattern(id->idSvePattern()); // ppppp
            code |= insEncodeUimm<19, 16>(imm - 1);          // iiii
            dst += emitOutput_Instr(dst, code);
            break;

        case IF_SVE_BS_1A: // ..............ii iiiiiiiiiiiddddd -- SVE bitwise logical with immediate (unpredicated)
        case IF_SVE_BT_1A: // ..............ii iiiiiiiiiiiddddd -- SVE broadcast bitmask immediate
            imm  = emitGetInsSC(id);
            code = emitInsCodeSve(ins, fmt);
            code |= insEncodeReg_V<4, 0>(id->idReg1()); // ddddd
            code |= (imm << 5);
            dst += emitOutput_Instr(dst, code);
            break;

        case IF_SVE_BU_2A: // ........xx..gggg ...iiiiiiiiddddd -- SVE copy floating-point immediate (predicated)
            imm  = emitGetInsSC(id);
            code = emitInsCodeSve(ins, fmt);
            code |= insEncodeReg_V<4, 0>(id->idReg1());                   // ddddd
            code |= insEncodeImm8_12_to_5(imm);                           // iiiiiiii
            code |= insEncodeReg_P<19, 16>(id->idReg2());                 // gggg
            code |= insEncodeElemsize(optGetSveElemsize(id->idInsOpt())); // xx
            dst += emitOutput_Instr(dst, code);
            break;

        case IF_SVE_BV_2A:   // ........xx..gggg ..hiiiiiiiiddddd -- SVE copy integer immediate (predicated)
        case IF_SVE_BV_2A_J: // ........xx..gggg ..hiiiiiiiiddddd -- SVE copy integer immediate (predicated)
            imm  = emitGetInsSC(id);
            code = emitInsCodeSve(ins, fmt);
            code |= insEncodeReg_V<4, 0>(id->idReg1());                   // ddddd
            code |= insEncodeReg_P<19, 16>(id->idReg2());                 // gggg
            code |= insEncodeImm8_12_to_5(imm);                           // iiiiiiii
            code |= (id->idHasShift() ? 0x2000 : 0);                      // h
            code |= insEncodeElemsize(optGetSveElemsize(id->idInsOpt())); // xx
            dst += emitOutput_Instr(dst, code);
            break;

        case IF_SVE_BV_2B: // ........xx..gggg ...........ddddd -- SVE copy integer immediate (predicated)
            // In emitIns, we set this format's instruction to MOV, as that is the preferred disassembly.
            // However, passing (MOV, IF_SVE_BV_2B) to emitInsCodeSve will assert with "encoding_found",
            // as FMOV is the only instruction associated with this encoding format.
            // Thus, always pass FMOV here, and use MOV elsewhere for simplicity.
            code = emitInsCodeSve(INS_sve_fmov, fmt);
            code |= insEncodeReg_V<4, 0>(id->idReg1());                   // ddddd
            code |= insEncodeReg_P<19, 16>(id->idReg2());                 // gggg
            code |= insEncodeElemsize(optGetSveElemsize(id->idInsOpt())); // xx
            dst += emitOutput_Instr(dst, code);
            break;

        case IF_SVE_BW_2A: // ........ii.xxxxx ......nnnnnddddd -- SVE broadcast indexed element
            imm  = emitGetInsSC(id);
            code = emitInsCodeSve(ins, fmt);
            code |= insEncodeReg_V<4, 0>(id->idReg1()); // ddddd
            code |= insEncodeReg_V<9, 5>(id->idReg2()); // nnnnn
            code |= insEncodeSveBroadcastIndex(optGetSveElemsize(id->idInsOpt()), imm);
            dst += emitOutput_Instr(dst, code);
            break;

        case IF_SVE_CE_2A: // ................ ......nnnnn.DDDD -- SVE move predicate from vector
            code = emitInsCodeSve(ins, fmt);
            code |= insEncodeReg_P<3, 0>(id->idReg1()); // DDDD
            code |= insEncodeReg_V<9, 5>(id->idReg2()); // nnnnn
            dst += emitOutput_Instr(dst, code);
            break;

        case IF_SVE_CE_2B: // .........i...ii. ......nnnnn.DDDD -- SVE move predicate from vector
            code = emitInsCodeSve(ins, fmt);
            code |= insEncodeReg_P<3, 0>(id->idReg1());                   // DDDD
            code |= insEncodeReg_V<9, 5>(id->idReg2());                   // nnnnn
            code |= insEncodeSplitUimm<22, 22, 18, 17>(emitGetInsSC(id)); // i...ii
            dst += emitOutput_Instr(dst, code);
            break;

        case IF_SVE_CE_2C: // ..............i. ......nnnnn.DDDD -- SVE move predicate from vector
            code = emitInsCodeSve(ins, fmt);
            code |= insEncodeReg_P<3, 0>(id->idReg1());      // DDDD
            code |= insEncodeReg_V<9, 5>(id->idReg2());      // nnnnn
            code |= insEncodeUimm<17, 17>(emitGetInsSC(id)); // i
            dst += emitOutput_Instr(dst, code);
            break;

        case IF_SVE_CE_2D: // .............ii. ......nnnnn.DDDD -- SVE move predicate from vector
            code = emitInsCodeSve(ins, fmt);
            code |= insEncodeReg_P<3, 0>(id->idReg1());      // DDDD
            code |= insEncodeReg_V<9, 5>(id->idReg2());      // nnnnn
            code |= insEncodeUimm<18, 17>(emitGetInsSC(id)); // ii
            dst += emitOutput_Instr(dst, code);
            break;

        case IF_SVE_CF_2A: // ................ .......NNNNddddd -- SVE move predicate into vector
            code = emitInsCodeSve(ins, fmt);
            code |= insEncodeReg_V<4, 0>(id->idReg1()); // ddddd
            code |= insEncodeReg_P<8, 5>(id->idReg2()); // NNNN
            dst += emitOutput_Instr(dst, code);
            break;

        case IF_SVE_CF_2B: // .........i...ii. .......NNNNddddd -- SVE move predicate into vector
            code = emitInsCodeSve(ins, fmt);
            code |= insEncodeReg_V<4, 0>(id->idReg1());                   // ddddd
            code |= insEncodeReg_P<8, 5>(id->idReg2());                   // NNNN
            code |= insEncodeSplitUimm<22, 22, 18, 17>(emitGetInsSC(id)); // i...ii
            dst += emitOutput_Instr(dst, code);
            break;

        case IF_SVE_CF_2C: // ..............i. .......NNNNddddd -- SVE move predicate into vector
            code = emitInsCodeSve(ins, fmt);
            code |= insEncodeReg_V<4, 0>(id->idReg1());      // ddddd
            code |= insEncodeReg_P<8, 5>(id->idReg2());      // NNNN
            code |= insEncodeUimm<17, 17>(emitGetInsSC(id)); // i
            dst += emitOutput_Instr(dst, code);
            break;

        case IF_SVE_CF_2D: // .............ii. .......NNNNddddd -- SVE move predicate into vector
            code = emitInsCodeSve(ins, fmt);
            code |= insEncodeReg_V<4, 0>(id->idReg1());      // ddddd
            code |= insEncodeReg_P<8, 5>(id->idReg2());      // NNNN
            code |= insEncodeUimm<18, 17>(emitGetInsSC(id)); // ii
            dst += emitOutput_Instr(dst, code);
            break;

        case IF_SVE_CC_2A: // ........xx...... ......mmmmmddddd -- SVE insert SIMD&FP scalar register
            code = emitInsCodeSve(ins, fmt);
            code |= insEncodeReg_V<4, 0>(id->idReg1());                      // ddddd
            code |= insEncodeReg_V<9, 5>(id->idReg2());                      // mmmmm
            code |= insEncodeSveElemsize(optGetSveElemsize(id->idInsOpt())); // xx
            dst += emitOutput_Instr(dst, code);
            break;

        case IF_SVE_CD_2A: // ........xx...... ......mmmmmddddd -- SVE insert general register
            code = emitInsCodeSve(ins, fmt);
            code |= insEncodeReg_V<4, 0>(id->idReg1());                      // ddddd
            code |= insEncodeReg_R<9, 5>(id->idReg2());                      // mmmmm
            code |= insEncodeSveElemsize(optGetSveElemsize(id->idInsOpt())); // xx
            dst += emitOutput_Instr(dst, code);
            break;

        case IF_SVE_CI_3A: // ........xx..MMMM .......NNNN.DDDD -- SVE permute predicate elements
            code = emitInsCodeSve(ins, fmt);
            code |= insEncodeReg_P<3, 0>(id->idReg1());                   // DDDD
            code |= insEncodeReg_P<8, 5>(id->idReg2());                   // NNNN
            code |= insEncodeReg_P<19, 16>(id->idReg3());                 // MMMM
            code |= insEncodeElemsize(optGetSveElemsize(id->idInsOpt())); // xx
            dst += emitOutput_Instr(dst, code);
            break;

        case IF_SVE_CJ_2A: // ........xx...... .......nnnn.dddd -- SVE reverse predicate elements
            code = emitInsCodeSve(ins, fmt);
            code |= insEncodeReg_P<3, 0>(id->idReg1());                   // DDDD
            code |= insEncodeReg_P<8, 5>(id->idReg2());                   // NNNN
            code |= insEncodeElemsize(optGetSveElemsize(id->idInsOpt())); // xx
            dst += emitOutput_Instr(dst, code);
            break;

        case IF_SVE_CK_2A: // ................ .......NNNN.DDDD -- SVE unpack predicate elements
            code = emitInsCodeSve(ins, fmt);
            code |= insEncodeReg_P<3, 0>(id->idReg1()); // DDDD
            code |= insEncodeReg_P<8, 5>(id->idReg2()); // NNNN
            dst += emitOutput_Instr(dst, code);
            break;

        case IF_SVE_GQ_3A: // ................ ...gggnnnnnddddd -- SVE floating-point convert precision odd elements
            code = emitInsCodeSve(ins, fmt);

            if (ins == INS_sve_fcvtnt && id->idInsOpt() == INS_OPTS_D_TO_S)
            {
                code |= (1 << 22 | 1 << 17);
            }
            else if (ins == INS_sve_fcvtlt && id->idInsOpt() == INS_OPTS_S_TO_D)
            {
                code |= (1 << 22 | 1 << 17);
            }

            code |= insEncodeReg_V<4, 0>(id->idReg1());   // ddddd
            code |= insEncodeReg_P<12, 10>(id->idReg2()); // ggg
            code |= insEncodeReg_V<9, 5>(id->idReg3());   // nnnnn
            dst += emitOutput_Instr(dst, code);
            break;

        // Scalable to general register.
        case IF_SVE_CO_3A: // ........xx...... ...gggmmmmmddddd -- SVE conditionally extract element to general register
        case IF_SVE_CS_3A: // ........xx...... ...gggnnnnnddddd -- SVE extract element to general register
            code = emitInsCodeSve(ins, fmt);
            code |= insEncodeReg_Rd(id->idReg1());                           // ddddd
            code |= insEncodeReg_P<12, 10>(id->idReg2());                    // ggg
            code |= insEncodeReg_V<9, 5>(id->idReg3());                      // mmmmm
            code |= insEncodeSveElemsize(optGetSveElemsize(id->idInsOpt())); // xx
            dst += emitOutput_Instr(dst, code);
            break;

        // Scalable from general register.
        case IF_SVE_CQ_3A: // ........xx...... ...gggnnnnnddddd -- SVE copy general register to vector (predicated)
            code = emitInsCodeSve(ins, fmt);
            code |= insEncodeReg_V<4, 0>(id->idReg1());                      // ddddd
            code |= insEncodeReg_P<12, 10>(id->idReg2());                    // ggg
            code |= insEncodeReg_Rn(id->idReg3());                           // mmmmm
            code |= insEncodeSveElemsize(optGetSveElemsize(id->idInsOpt())); // xx
            dst += emitOutput_Instr(dst, code);
            break;

        case IF_SVE_CT_3A: // ................ ...gggnnnnnddddd -- SVE reverse doublewords
            code = emitInsCodeSve(ins, fmt);
            code |= insEncodeReg_V<4, 0>(id->idReg1());   // ddddd
            code |= insEncodeReg_P<12, 10>(id->idReg2()); // ggg
            code |= insEncodeReg_V<9, 5>(id->idReg3());   // nnnnn
            dst += emitOutput_Instr(dst, code);
            break;

        case IF_SVE_CV_3A: // ........xx...... ...VVVnnnnnddddd -- SVE vector splice (constructive)
        case IF_SVE_CV_3B: // ........xx...... ...VVVmmmmmddddd -- SVE vector splice (destructive)
            code = emitInsCodeSve(ins, fmt);
            code |= insEncodeReg_V<4, 0>(id->idReg1());                   // ddddd
            code |= insEncodeReg_P<12, 10>(id->idReg2());                 // VVV
            code |= insEncodeReg_V<9, 5>(id->idReg3());                   // nnnnn/mmmmm
            code |= insEncodeElemsize(optGetSveElemsize(id->idInsOpt())); // xx
            dst += emitOutput_Instr(dst, code);
            break;

        case IF_SVE_CW_4A: // ........xx.mmmmm ..VVVVnnnnnddddd -- SVE select vector elements (predicated)
        {
            regNumber reg4 = (ins == INS_sve_mov ? id->idReg1() : id->idReg4());
            code           = emitInsCodeSve(ins, fmt);
            code |= insEncodeReg_V<4, 0>(id->idReg1());                   // ddddd
            code |= insEncodeReg_P<13, 10>(id->idReg2());                 // VVVV
            code |= insEncodeReg_V<9, 5>(id->idReg3());                   // nnnnn
            code |= insEncodeReg_V<20, 16>(reg4);                         // mmmmm
            code |= insEncodeElemsize(optGetSveElemsize(id->idInsOpt())); // xx
            dst += emitOutput_Instr(dst, code);
            break;
        }

        case IF_SVE_CX_4A:   // ........xx.mmmmm ...gggnnnnn.DDDD -- SVE integer compare vectors
        case IF_SVE_CX_4A_A: // ........xx.mmmmm ...gggnnnnn.DDDD -- SVE integer compare vectors
        case IF_SVE_GE_4A:   // ........xx.mmmmm ...gggnnnnn.DDDD -- SVE2 character match
        case IF_SVE_HT_4A:   // ........xx.mmmmm ...gggnnnnn.DDDD -- SVE floating-point compare vectors
            code = emitInsCodeSve(ins, fmt);
            code |= insEncodeReg_P<3, 0>(id->idReg1());                   // DDDD
            code |= insEncodeReg_P<12, 10>(id->idReg2());                 // ggg
            code |= insEncodeReg_V<9, 5>(id->idReg3());                   // mmmmm
            code |= insEncodeReg_V<20, 16>(id->idReg4());                 // nnnnn
            code |= insEncodeElemsize(optGetSveElemsize(id->idInsOpt())); // xx
            dst += emitOutput_Instr(dst, code);
            break;

        case IF_SVE_CY_3A: // ........xx.iiiii ...gggnnnnn.DDDD -- SVE integer compare with signed immediate
            imm  = emitGetInsSC(id);
            code = emitInsCodeSve(ins, fmt);
            code |= insEncodeReg_P<3, 0>(id->idReg1());                   // DDDD
            code |= insEncodeReg_P<12, 10>(id->idReg2());                 // ggg
            code |= insEncodeReg_V<9, 5>(id->idReg3());                   // nnnnn
            code |= insEncodeSimm<20, 16>(imm);                           // iiiii
            code |= insEncodeElemsize(optGetSveElemsize(id->idInsOpt())); // xx
            dst += emitOutput_Instr(dst, code);
            break;

        case IF_SVE_CY_3B: // ........xx.iiiii ii.gggnnnnn.DDDD -- SVE integer compare with unsigned immediate
            imm  = emitGetInsSC(id);
            code = emitInsCodeSve(ins, fmt);
            code |= insEncodeReg_P<3, 0>(id->idReg1());                   // DDDD
            code |= insEncodeReg_P<12, 10>(id->idReg2());                 // ggg
            code |= insEncodeReg_V<9, 5>(id->idReg3());                   // nnnnn
            code |= insEncodeUimm<20, 14>(imm);                           // iiiii
            code |= insEncodeElemsize(optGetSveElemsize(id->idInsOpt())); // xx
            dst += emitOutput_Instr(dst, code);
            break;

        case IF_SVE_EW_3A:   // ...........mmmmm ......nnnnnddddd -- SVE2 multiply-add (checked pointer)
        case IF_SVE_BR_3B:   // ...........mmmmm ......nnnnnddddd -- SVE permute vector segments
        case IF_SVE_FN_3B:   // ...........mmmmm ......nnnnnddddd -- SVE2 integer multiply long
        case IF_SVE_FO_3A:   // ...........mmmmm ......nnnnnddddd -- SVE integer matrix multiply accumulate
        case IF_SVE_AT_3B:   // ...........mmmmm ......nnnnnddddd -- SVE integer add/subtract vectors (unpredicated)
        case IF_SVE_BD_3B:   // ...........mmmmm ......nnnnnddddd -- SVE2 integer multiply vectors (unpredicated)
        case IF_SVE_EF_3A:   // ...........mmmmm ......nnnnnddddd -- SVE two-way dot product
        case IF_SVE_EI_3A:   // ...........mmmmm ......nnnnnddddd -- SVE mixed sign dot product
        case IF_SVE_GJ_3A:   // ...........mmmmm ......nnnnnddddd -- SVE2 crypto constructive binary operations
        case IF_SVE_GN_3A:   // ...........mmmmm ......nnnnnddddd -- SVE2 FP8 multiply-add long
        case IF_SVE_GO_3A:   // ...........mmmmm ......nnnnnddddd -- SVE2 FP8 multiply-add long long
        case IF_SVE_GW_3B:   // ...........mmmmm ......nnnnnddddd -- SVE FP clamp
        case IF_SVE_HA_3A:   // ...........mmmmm ......nnnnnddddd -- SVE BFloat16 floating-point dot product
        case IF_SVE_HA_3A_E: // ...........mmmmm ......nnnnnddddd -- SVE BFloat16 floating-point dot product
        case IF_SVE_HA_3A_F: // ...........mmmmm ......nnnnnddddd -- SVE BFloat16 floating-point dot product
        case IF_SVE_HB_3A:   // ...........mmmmm ......nnnnnddddd -- SVE floating-point multiply-add long
        case IF_SVE_HD_3A:   // ...........mmmmm ......nnnnnddddd -- SVE floating point matrix multiply accumulate
        case IF_SVE_HD_3A_A: // ...........mmmmm ......nnnnnddddd -- SVE floating point matrix multiply accumulate
        case IF_SVE_HK_3B:   // ...........mmmmm ......nnnnnddddd -- SVE floating-point arithmetic (unpredicated)
            code = emitInsCodeSve(ins, fmt);
            code |= insEncodeReg_V<4, 0>(id->idReg1());   // ddddd
            code |= insEncodeReg_V<9, 5>(id->idReg2());   // nnnnn
            code |= insEncodeReg_V<20, 16>(id->idReg3()); // mmmmm
            dst += emitOutput_Instr(dst, code);
            break;

        case IF_SVE_AU_3A: // ...........mmmmm ......nnnnnddddd -- SVE bitwise logical operations (unpredicated)
            code = emitInsCodeSve(ins, fmt);
            code |= insEncodeReg_V<4, 0>(id->idReg1()); // ddddd
            code |= insEncodeReg_V<9, 5>(id->idReg2()); // nnnnn
            if (id->idIns() != INS_sve_mov)
            {
                code |= insEncodeReg_V<20, 16>(id->idReg3()); // mmmmm
            }
            dst += emitOutput_Instr(dst, code);
            break;

        case IF_SVE_AV_3A: // ...........mmmmm ......kkkkkddddd -- SVE2 bitwise ternary operations
            code = emitInsCodeSve(ins, fmt);
            code |= insEncodeReg_V<4, 0>(id->idReg1());   // ddddd
            code |= insEncodeReg_V<20, 16>(id->idReg2()); // mmmmm
            code |= insEncodeReg_V<9, 5>(id->idReg3());   // kkkkk
            dst += emitOutput_Instr(dst, code);
            break;

        case IF_SVE_AW_2A: // ........xx.xxiii ......mmmmmddddd -- sve_int_rotate_imm
            imm  = insSveGetImmDiff(emitGetInsSC(id), id->idInsOpt());
            code = emitInsCodeSve(ins, fmt);
            code |= insEncodeReg_V<4, 0>(id->idReg1());                                            // ddddd
            code |= insEncodeReg_V<9, 5>(id->idReg2());                                            // mmmmm
            code |= insEncodeUimm<20, 16>(imm & 0b11111);                                          // xxiii
            code |= insEncodeUimm<22, 22>(imm >> 5);                                               // x
            code |= insEncodeSveElemsize_tszh_23_tszl_20_to_19(optGetSveElemsize(id->idInsOpt())); // xx xx
            dst += emitOutput_Instr(dst, code);
            break;

        case IF_SVE_AX_1A: // ........xx.iiiii ......iiiiiddddd -- SVE index generation (immediate start, immediate
                           // increment)
        {
            ssize_t imm1;
            ssize_t imm2;
            insSveDecodeTwoSimm5(emitGetInsSC(id), &imm1, &imm2);
            code = emitInsCodeSve(ins, fmt);
            code |= insEncodeReg_V<4, 0>(id->idReg1());                   // ddddd
            code |= insEncodeSimm<9, 5>(imm1);                            // iiiii
            code |= insEncodeSimm<20, 16>(imm2);                          // iiiii
            code |= insEncodeElemsize(optGetSveElemsize(id->idInsOpt())); // xx
            dst += emitOutput_Instr(dst, code);
            break;
        }

        case IF_SVE_AY_2A: // ........xx.mmmmm ......iiiiiddddd -- SVE index generation (immediate start, register
                           // increment)
            code = emitInsCodeSve(ins, fmt);
            code |= insEncodeReg_V<4, 0>(id->idReg1());                   // ddddd
            code |= insEncodeSimm<9, 5>(emitGetInsSC(id));                // iiiii
            code |= insEncodeReg_R<20, 16>(id->idReg2());                 // mmmmm
            code |= insEncodeElemsize(optGetSveElemsize(id->idInsOpt())); // xx
            dst += emitOutput_Instr(dst, code);
            break;

        case IF_SVE_AZ_2A: // ........xx.iiiii ......nnnnnddddd -- SVE index generation (register start, immediate
                           // increment)
            code = emitInsCodeSve(ins, fmt);
            code |= insEncodeReg_V<4, 0>(id->idReg1());                   // ddddd
            code |= insEncodeReg_R<9, 5>(id->idReg2());                   // mmmmm
            code |= insEncodeSimm<20, 16>(emitGetInsSC(id));              // iiiii
            code |= insEncodeElemsize(optGetSveElemsize(id->idInsOpt())); // xx
            dst += emitOutput_Instr(dst, code);
            break;

        case IF_SVE_BB_2A: // ...........nnnnn .....iiiiiiddddd -- SVE stack frame adjustment
            code = emitInsCodeSve(ins, fmt);
            code |= insEncodeReg_R<4, 0>(id->idReg1());     // ddddd
            code |= insEncodeSimm<10, 5>(emitGetInsSC(id)); // iiiiii
            code |= insEncodeReg_R<20, 16>(id->idReg2());   // nnnnn
            dst += emitOutput_Instr(dst, code);
            break;

        case IF_SVE_BC_1A: // ................ .....iiiiiiddddd -- SVE stack frame size
            code = emitInsCodeSve(ins, fmt);
            code |= insEncodeReg_R<4, 0>(id->idReg1());     // ddddd
            code |= insEncodeSimm<10, 5>(emitGetInsSC(id)); // iiiiii
            dst += emitOutput_Instr(dst, code);
            break;

        case IF_SVE_EW_3B: // ...........mmmmm ......aaaaaddddd -- SVE2 multiply-add (checked pointer)
            code = emitInsCodeSve(ins, fmt);
            code |= insEncodeReg_V<4, 0>(id->idReg1());   // ddddd
            code |= insEncodeReg_V<9, 5>(id->idReg3());   // aaaaa
            code |= insEncodeReg_V<20, 16>(id->idReg2()); // mmmmm
            dst += emitOutput_Instr(dst, code);
            break;

        case IF_SVE_EG_3A:   // ...........iimmm ......nnnnnddddd -- SVE two-way dot product (indexed)
        case IF_SVE_EY_3A:   // ...........iimmm ......nnnnnddddd -- SVE integer dot product (indexed)
        case IF_SVE_EZ_3A:   // ...........iimmm ......nnnnnddddd -- SVE mixed sign dot product (indexed)
        case IF_SVE_FD_3B:   // ...........iimmm ......nnnnnddddd -- SVE2 integer multiply (indexed)
        case IF_SVE_FF_3B:   // ...........iimmm ......nnnnnddddd -- SVE2 integer multiply-add (indexed)
        case IF_SVE_FI_3B:   // ...........iimmm ......nnnnnddddd -- SVE2 saturating multiply high (indexed)
        case IF_SVE_GU_3A:   // ...........iimmm ......nnnnnddddd -- SVE floating-point multiply-add (indexed)
        case IF_SVE_GX_3A:   // ...........iimmm ......nnnnnddddd -- SVE floating-point multiply (indexed)
        case IF_SVE_GY_3B:   // ...........iimmm ......nnnnnddddd -- SVE BFloat16 floating-point dot product (indexed)
        case IF_SVE_GY_3B_D: // ...........iimmm ......nnnnnddddd -- SVE BFloat16 floating-point dot product (indexed)
        case IF_SVE_FK_3B:   // ...........iimmm ......nnnnnddddd -- SVE2 saturating multiply-add high (indexed)
            code = emitInsCodeSve(ins, fmt);
            code |= insEncodeReg_V<4, 0>(id->idReg1());      // ddddd
            code |= insEncodeReg_V<9, 5>(id->idReg2());      // nnnnn
            code |= insEncodeReg_V<18, 16>(id->idReg3());    // mmm
            code |= insEncodeUimm<20, 19>(emitGetInsSC(id)); // ii
            dst += emitOutput_Instr(dst, code);
            break;

        case IF_SVE_FD_3A: // .........i.iimmm ......nnnnnddddd -- SVE2 integer multiply (indexed)
        case IF_SVE_FF_3A: // .........i.iimmm ......nnnnnddddd -- SVE2 integer multiply-add (indexed)
        case IF_SVE_FI_3A: // .........i.iimmm ......nnnnnddddd -- SVE2 saturating multiply high (indexed)
        case IF_SVE_GU_3C: // .........i.iimmm ......nnnnnddddd -- SVE floating-point multiply-add (indexed)
        case IF_SVE_GX_3C: // .........i.iimmm ......nnnnnddddd -- SVE floating-point multiply (indexed)
        case IF_SVE_FK_3A: // .........i.iimmm ......nnnnnddddd -- SVE2 saturating multiply-add high (indexed)
            imm  = emitGetInsSC(id);
            code = emitInsCodeSve(ins, fmt);
            code |= insEncodeReg_V<4, 0>(id->idReg1());   // ddddd
            code |= insEncodeReg_V<9, 5>(id->idReg2());   // nnnnn
            code |= insEncodeReg_V<18, 16>(id->idReg3()); // mmm
            code |= insEncodeUimm<20, 19>(imm & 0b11);    // ii
            code |= insEncodeUimm<22, 22>(imm >> 2);      // i
            dst += emitOutput_Instr(dst, code);
            break;

        case IF_SVE_FE_3A: // ...........iimmm ....i.nnnnnddddd -- SVE2 integer multiply long (indexed)
        case IF_SVE_FG_3A: // ...........iimmm ....i.nnnnnddddd -- SVE2 integer multiply-add long (indexed)
        case IF_SVE_FH_3A: // ...........iimmm ....i.nnnnnddddd -- SVE2 saturating multiply (indexed)
        case IF_SVE_FJ_3A: // ...........iimmm ....i.nnnnnddddd -- SVE2 saturating multiply-add (indexed)
        case IF_SVE_GY_3A: // ...........iimmm ....i.nnnnnddddd -- SVE BFloat16 floating-point dot product (indexed)
        case IF_SVE_GZ_3A: // ...........iimmm ....i.nnnnnddddd -- SVE floating-point multiply-add long (indexed)
            imm  = emitGetInsSC(id);
            code = emitInsCodeSve(ins, fmt);
            code |= insEncodeReg_V<4, 0>(id->idReg1());   // ddddd
            code |= insEncodeReg_V<9, 5>(id->idReg2());   // nnnnn
            code |= insEncodeUimm<11, 11>(imm & 1);       // i
            code |= insEncodeReg_V<18, 16>(id->idReg3()); // mmm
            code |= insEncodeUimm<20, 19>(imm >> 1);      // ii
            dst += emitOutput_Instr(dst, code);
            break;

        case IF_SVE_FE_3B: // ...........immmm ....i.nnnnnddddd -- SVE2 integer multiply long (indexed)
        case IF_SVE_FG_3B: // ...........immmm ....i.nnnnnddddd -- SVE2 integer multiply-add long (indexed)
        case IF_SVE_FH_3B: // ...........immmm ....i.nnnnnddddd -- SVE2 saturating multiply (indexed)
        case IF_SVE_FJ_3B: // ...........immmm ....i.nnnnnddddd -- SVE2 saturating multiply-add (indexed)
            imm  = emitGetInsSC(id);
            code = emitInsCodeSve(ins, fmt);
            code |= insEncodeReg_V<4, 0>(id->idReg1());   // ddddd
            code |= insEncodeReg_V<9, 5>(id->idReg2());   // nnnnn
            code |= insEncodeUimm<11, 11>(imm & 1);       // i
            code |= insEncodeReg_V<19, 16>(id->idReg3()); // mmmm
            code |= insEncodeUimm<20, 19>(imm & 0b10);    // i
            dst += emitOutput_Instr(dst, code);
            break;

        case IF_SVE_EY_3B: // ...........immmm ......nnnnnddddd -- SVE integer dot product (indexed)
        case IF_SVE_FD_3C: // ...........immmm ......nnnnnddddd -- SVE2 integer multiply (indexed)
        case IF_SVE_FF_3C: // ...........immmm ......nnnnnddddd -- SVE2 integer multiply-add (indexed)
        case IF_SVE_FI_3C: // ...........immmm ......nnnnnddddd -- SVE2 saturating multiply high (indexed)
        case IF_SVE_GU_3B: // ...........immmm ......nnnnnddddd -- SVE floating-point multiply-add (indexed)
        case IF_SVE_GX_3B: // ...........immmm ......nnnnnddddd -- SVE floating-point multiply (indexed)
        case IF_SVE_FK_3C: // ...........immmm ......nnnnnddddd -- SVE2 saturating multiply-add high (indexed)
            code = emitInsCodeSve(ins, fmt);
            code |= insEncodeReg_V<4, 0>(id->idReg1());   // ddddd
            code |= insEncodeReg_V<9, 5>(id->idReg2());   // nnnnn
            code |= insEncodeReg_V<19, 16>(id->idReg3()); // mmmm

            // index is encoded at bit location 20;
            // left-shift by one bit so we can reuse insEncodeUimm<20, 19> without modifying bit location 19
            code |= insEncodeUimm<20, 19>(emitGetInsSC(id) << 1); // i
            dst += emitOutput_Instr(dst, code);
            break;

        case IF_SVE_CZ_4A: // ............MMMM ..gggg.NNNN.DDDD -- SVE predicate logical operations
        case IF_SVE_DA_4A: // ............MMMM ..gggg.NNNN.DDDD -- SVE propagate break from previous partition
        {
            code = emitInsCodeSve(ins, fmt);
            code |= insEncodeReg_P<3, 0>(id->idReg1());   // DDDD
            code |= insEncodeReg_P<13, 10>(id->idReg2()); // gggg
            code |= insEncodeReg_P<8, 5>(id->idReg3());   // NNNN

            regNumber regm;
            switch (ins)
            {
                case INS_sve_mov:
                case INS_sve_movs:
                    regm = id->idReg3();
                    break;

                case INS_sve_not:
                case INS_sve_nots:
                    regm = id->idReg2();
                    break;

                default:
                    regm = id->idReg4();
            }

            code |= insEncodeReg_P<19, 16>(regm); // MMMM
            dst += emitOutput_Instr(dst, code);
            break;
        }

        case IF_SVE_CZ_4A_A: // ............MMMM ..gggg.NNNN.DDDD -- SVE predicate logical operations
        case IF_SVE_CZ_4A_L: // ............MMMM ..gggg.NNNN.DDDD -- SVE predicate logical operations
            code = emitInsCodeSve(ins, fmt);
            code |= insEncodeReg_P<3, 0>(id->idReg1());   // DDDD
            code |= insEncodeReg_P<13, 10>(id->idReg2()); // NNNN
            code |= insEncodeReg_P<8, 5>(id->idReg2());   // NNNN
            code |= insEncodeReg_P<19, 16>(id->idReg2()); // NNNN
            dst += emitOutput_Instr(dst, code);
            break;

        case IF_SVE_CZ_4A_K: // ............MMMM ..gggg.NNNN.DDDD -- SVE predicate logical operations
            code = emitInsCodeSve(ins, fmt);
            code |= insEncodeReg_P<3, 0>(id->idReg1());   // DDDD
            code |= insEncodeReg_P<13, 10>(id->idReg2()); // gggg
            code |= insEncodeReg_P<8, 5>(id->idReg3());   // NNNN
            code |= insEncodeReg_P<19, 16>(id->idReg1()); // DDDD
            dst += emitOutput_Instr(dst, code);
            break;

        case IF_SVE_DB_3A: // ................ ..gggg.NNNNMDDDD -- SVE partition break condition
            code = emitInsCodeSve(ins, fmt);
            code |= insEncodeReg_P<3, 0>(id->idReg1());                   // DDDD
            code |= insEncodeReg_P<13, 10>(id->idReg2());                 // gggg
            code |= insEncodeReg_P<8, 5>(id->idReg3());                   // NNNN
            code |= insEncodePredQualifier_4(id->idPredicateReg2Merge()); // M
            dst += emitOutput_Instr(dst, code);
            break;

        case IF_SVE_DB_3B: // ................ ..gggg.NNNN.DDDD -- SVE partition break condition
        case IF_SVE_DC_3A: // ................ ..gggg.NNNN.MMMM -- SVE propagate break to next partition
            code = emitInsCodeSve(ins, fmt);
            code |= insEncodeReg_P<3, 0>(id->idReg1());   // DDDD
            code |= insEncodeReg_P<13, 10>(id->idReg2()); // gggg
            code |= insEncodeReg_P<8, 5>(id->idReg3());   // NNNN
            dst += emitOutput_Instr(dst, code);
            break;

        case IF_SVE_DD_2A: // ................ .......gggg.DDDD -- SVE predicate first active
        case IF_SVE_DG_2A: // ................ .......gggg.DDDD -- SVE predicate read from FFR (predicated)
            code = emitInsCodeSve(ins, fmt);
            code |= insEncodeReg_P<3, 0>(id->idReg1()); // DDDD
            code |= insEncodeReg_P<8, 5>(id->idReg2()); // gggg
            dst += emitOutput_Instr(dst, code);
            break;

        case IF_SVE_DE_1A: // ........xx...... ......ppppp.DDDD -- SVE predicate initialize
            code = emitInsCodeSve(ins, fmt);
            code |= insEncodeSvePattern(id->idSvePattern());              // ppppp
            code |= insEncodeReg_P<3, 0>(id->idReg1());                   // DDDD
            code |= insEncodeElemsize(optGetSveElemsize(id->idInsOpt())); // xx
            dst += emitOutput_Instr(dst, code);
            break;

        case IF_SVE_DF_2A: // ........xx...... .......VVVV.DDDD -- SVE predicate next active
            code = emitInsCodeSve(ins, fmt);
            code |= insEncodeReg_P<3, 0>(id->idReg1());                   // DDDD
            code |= insEncodeReg_P<8, 5>(id->idReg2());                   // VVVV
            code |= insEncodeElemsize(optGetSveElemsize(id->idInsOpt())); // xx
            dst += emitOutput_Instr(dst, code);
            break;

        case IF_SVE_DH_1A: // ................ ............DDDD -- SVE predicate read from FFR (unpredicated)
        case IF_SVE_DJ_1A: // ................ ............DDDD -- SVE predicate zero
            code = emitInsCodeSve(ins, fmt);
            code |= insEncodeReg_P<3, 0>(id->idReg1()); // DDDD
            dst += emitOutput_Instr(dst, code);
            break;

        case IF_SVE_DI_2A: // ................ ..gggg.NNNN..... -- SVE predicate test
            code = emitInsCodeSve(ins, fmt);
            code |= insEncodeReg_P<13, 10>(id->idReg1()); // gggg
            code |= insEncodeReg_P<8, 5>(id->idReg2());   // NNNN
            dst += emitOutput_Instr(dst, code);
            break;

        case IF_SVE_DK_3A: // ........xx...... ..gggg.NNNNddddd -- SVE predicate count
            code = emitInsCodeSve(ins, fmt);
            code |= insEncodeReg_R<4, 0>(id->idReg1());                   // ddddd
            code |= insEncodeReg_P<13, 10>(id->idReg2());                 // gggg
            code |= insEncodeReg_P<8, 5>(id->idReg3());                   // NNNN
            code |= insEncodeElemsize(optGetSveElemsize(id->idInsOpt())); // xx
            dst += emitOutput_Instr(dst, code);
            break;

        case IF_SVE_GA_2A: // ............iiii ......nnnn.ddddd -- SME2 multi-vec shift narrow
            imm = emitGetInsSC(id);
            assert(id->idInsOpt() == INS_OPTS_SCALABLE_H);
            assert(emitInsIsVectorRightShift(id->idIns()));
            assert(isValidVectorShiftAmount(imm, EA_4BYTE, /* rightShift */ true));
            code = emitInsCodeSve(ins, fmt);
            code |= insEncodeVectorShift(EA_4BYTE, true /* right-shift */, imm); // iiii
            code |= insEncodeReg_V<4, 0>(id->idReg1());                          // ddddd
            code |= insEncodeReg_V_9_to_6_Times_Two(id->idReg2());               // nnnn
            dst += emitOutput_Instr(dst, code);
            break;

        case IF_SVE_DL_2A: // ........xx...... .....l.NNNNddddd -- SVE predicate count (predicate-as-counter)
            code = emitInsCodeSve(ins, fmt);
            code |= insEncodeVectorLengthSpecifier(id);                      // l
            code |= insEncodeReg_R<4, 0>(id->idReg1());                      // ddddd
            code |= insEncodeReg_P<8, 5>(id->idReg2());                      // NNNN
            code |= insEncodeSveElemsize(optGetSveElemsize(id->idInsOpt())); // xx
            dst += emitOutput_Instr(dst, code);
            break;

        case IF_SVE_DM_2A: // ........xx...... .......MMMMddddd -- SVE inc/dec register by predicate count
            code = emitInsCodeSve(ins, fmt);
            code |= insEncodeReg_R<4, 0>(id->idReg1());                      // ddddd
            code |= insEncodeReg_P<8, 5>(id->idReg2());                      // MMMM
            code |= insEncodeSveElemsize(optGetSveElemsize(id->idInsOpt())); // xx
            dst += emitOutput_Instr(dst, code);
            break;

        case IF_SVE_DN_2A: // ........xx...... .......MMMMddddd -- SVE inc/dec vector by predicate count
        case IF_SVE_DP_2A: // ........xx...... .......MMMMddddd -- SVE saturating inc/dec vector by predicate count
            code = emitInsCodeSve(ins, fmt);
            code |= insEncodeReg_V<4, 0>(id->idReg1());                      // ddddd
            code |= insEncodeReg_P<8, 5>(id->idReg2());                      // MMMM
            code |= insEncodeSveElemsize(optGetSveElemsize(id->idInsOpt())); // xx
            dst += emitOutput_Instr(dst, code);
            break;

        case IF_SVE_DO_2A: // ........xx...... .....X.MMMMddddd -- SVE saturating inc/dec register by predicate count
            code = emitInsCodeSve(ins, fmt);
            code |= insEncodeReg_R<4, 0>(id->idReg1());                      // ddddd
            code |= insEncodeReg_P<8, 5>(id->idReg2());                      // MMMM
            code |= insEncodeVLSElemsize(id->idOpSize());                    // X
            code |= insEncodeSveElemsize(optGetSveElemsize(id->idInsOpt())); // xx
            dst += emitOutput_Instr(dst, code);
            break;

        case IF_SVE_DQ_0A: // ................ ................ -- SVE FFR initialise
            code = emitInsCodeSve(ins, fmt);
            dst += emitOutput_Instr(dst, code);
            break;

        case IF_SVE_DR_1A: // ................ .......NNNN..... -- SVE FFR write from predicate
            code = emitInsCodeSve(ins, fmt);
            code |= insEncodeReg_P<8, 5>(id->idReg1()); // NNNN
            dst += emitOutput_Instr(dst, code);
            break;

        case IF_SVE_DS_2A: // .........x.mmmmm ......nnnnn..... -- SVE conditionally terminate scalars
            code = emitInsCodeSve(ins, fmt);
            code |= insEncodeReg_R<9, 5>(id->idReg1());        // nnnnn
            code |= insEncodeReg_R<20, 16>(id->idReg2());      // mmmmm
            code |= insEncodeSveElemsize_R_22(id->idOpSize()); // x
            dst += emitOutput_Instr(dst, code);
            break;

        case IF_SVE_FZ_2A: // ................ ......nnnn.ddddd -- SME2 multi-vec extract narrow
        case IF_SVE_HG_2A: // ................ ......nnnn.ddddd -- SVE2 FP8 downconverts
            code = emitInsCodeSve(ins, fmt);
            code |= insEncodeReg_V<4, 0>(id->idReg1());            // ddddd
            code |= insEncodeReg_V_9_to_6_Times_Two(id->idReg2()); // nnnn
            dst += emitOutput_Instr(dst, code);
            break;

        case IF_SVE_GD_2A: // .........x.xx... ......nnnnnddddd -- SVE2 saturating extract narrow
            code = emitInsCodeSve(ins, fmt);
            code |= insEncodeReg_V<4, 0>(id->idReg1()); // ddddd
            code |= insEncodeReg_V<9, 5>(id->idReg2()); // nnnnn
            // Bit 23 should not be set by below call
            assert(insOptsScalableWide(id->idInsOpt()));
            code |= insEncodeSveElemsize_tszh_23_tszl_20_to_19(optGetSveElemsize(id->idInsOpt())); // xx
                                                                                                   // x
            dst += emitOutput_Instr(dst, code);
            break;

        case IF_SVE_FR_2A: // .........x.xxiii ......nnnnnddddd -- SVE2 bitwise shift left long
            code = emitInsCodeSve(ins, fmt);
            code |= insEncodeReg_V<4, 0>(id->idReg1());      // ddddd
            code |= insEncodeReg_V<9, 5>(id->idReg2());      // nnnnn
            code |= insEncodeUimm<20, 16>(emitGetInsSC(id)); // iii
            // Bit 23 should not be set by below call
            assert(insOptsScalableWide(id->idInsOpt()));
            code |= insEncodeSveElemsize_tszh_23_tszl_20_to_19(optGetSveElemsize(id->idInsOpt())); // xx
                                                                                                   // x
            dst += emitOutput_Instr(dst, code);
            break;

        case IF_SVE_GB_2A: // .........x.xxiii ......nnnnnddddd -- SVE2 bitwise shift right narrow
            // Bit 23 should not be set by call to insEncodeSveElemsize_tszh_23_tszl_20_to_19,
            // nor should we pass INS_OPTS_SCALABLE_D to insGetImmDiff.
            assert(insOptsScalableWide(id->idInsOpt()));
            code = emitInsCodeSve(ins, fmt);
            code |= insEncodeReg_V<4, 0>(id->idReg1());                                            // ddddd
            code |= insEncodeReg_V<9, 5>(id->idReg2());                                            // nnnnn
            code |= insEncodeUimm<20, 16>(insSveGetImmDiff(emitGetInsSC(id), id->idInsOpt()));     // iii
            code |= insEncodeSveElemsize_tszh_23_tszl_20_to_19(optGetSveElemsize(id->idInsOpt())); // xx
                                                                                                   // x
            dst += emitOutput_Instr(dst, code);
            break;

        case IF_SVE_FV_2A: // ........xx...... .....rmmmmmddddd -- SVE2 complex integer add
            code = emitInsCodeSve(ins, fmt);
            code |= insEncodeReg_V<4, 0>(id->idReg1());                   // ddddd
            code |= insEncodeReg_V<9, 5>(id->idReg2());                   // mmmmm
            code |= insEncodeUimm<10, 10>(emitGetInsSC(id));              // r
            code |= insEncodeElemsize(optGetSveElemsize(id->idInsOpt())); // xx
            dst += emitOutput_Instr(dst, code);
            break;

        case IF_SVE_FY_3A: // .........x.mmmmm ......nnnnnddddd -- SVE2 integer add/subtract long with carry
        {
            // Size encoding: 1 if INS_OPTS_SCALABLE_D, 0 if INS_OPTS_SCALABLE_S
            const ssize_t sizeEncoding = (id->idInsOpt() == INS_OPTS_SCALABLE_D) ? 1 : 0;
            code                       = emitInsCodeSve(ins, fmt);
            code |= insEncodeReg_V<4, 0>(id->idReg1());   // ddddd
            code |= insEncodeReg_V<9, 5>(id->idReg2());   // nnnnn
            code |= insEncodeReg_V<20, 16>(id->idReg3()); // mmmmm
            code |= insEncodeUimm<22, 22>(sizeEncoding);  // x
            dst += emitOutput_Instr(dst, code);
            break;
        }

        case IF_SVE_GK_2A: // ................ ......mmmmmddddd -- SVE2 crypto destructive binary operations
            code = emitInsCodeSve(ins, fmt);
            code |= insEncodeReg_V<4, 0>(id->idReg1()); // ddddd
            code |= insEncodeReg_V<9, 5>(id->idReg2()); // mmmmm
            dst += emitOutput_Instr(dst, code);
            break;

        case IF_SVE_GL_1A: // ................ ...........ddddd -- SVE2 crypto unary operations
            code = emitInsCodeSve(ins, fmt);
            code |= insEncodeReg_V<4, 0>(id->idReg1()); // ddddd
            dst += emitOutput_Instr(dst, code);
            break;

        case IF_SVE_DT_3A: // ........xx.mmmmm ...X..nnnnn.DDDD -- SVE integer compare scalar count and limit
            code = emitInsCodeSve(ins, fmt);
            code |= insEncodeReg_P<3, 0>(id->idReg1());                      // DDDD
            code |= insEncodeReg_R<9, 5>(id->idReg2());                      // nnnnn
            code |= (id->idOpSize() == EA_8BYTE) ? (1 << 12) : 0;            // X
            code |= insEncodeReg_R<20, 16>(id->idReg3());                    // mmmmm
            code |= insEncodeSveElemsize(optGetSveElemsize(id->idInsOpt())); // xx
            dst += emitOutput_Instr(dst, code);
            break;

        case IF_SVE_DW_2A: // ........xx...... ......iiNNN.DDDD -- SVE extract mask predicate from predicate-as-counter
        case IF_SVE_DW_2B: // ........xx...... .......iNNN.DDDD -- SVE extract mask predicate from predicate-as-counter
            code = emitInsCodeSve(ins, fmt);
            code |= insEncodeReg_P<3, 0>(id->idReg1());                   // DDDD
            code |= insEncodeReg_P<7, 5>(id->idReg2());                   // NNN
            code |= insEncodeUimm<9, 8>(emitGetInsSC(id));                // ii (or i)
            code |= insEncodeElemsize(optGetSveElemsize(id->idInsOpt())); // xx
            dst += emitOutput_Instr(dst, code);
            break;

        case IF_SVE_DX_3A: // ........xx.mmmmm ......nnnnn.DDD. -- SVE integer compare scalar count and limit (predicate
                           // pair)
            code = emitInsCodeSve(ins, fmt);
            code |= insEncodeReg_P<3, 1>(id->idReg1());                      // DDD
            code |= insEncodeReg_R<9, 5>(id->idReg2());                      // nnnnn
            code |= insEncodeReg_R<20, 16>(id->idReg3());                    // mmmmm
            code |= insEncodeSveElemsize(optGetSveElemsize(id->idInsOpt())); // xx
            dst += emitOutput_Instr(dst, code);
            break;

        case IF_SVE_DY_3A: // ........xx.mmmmm ..l...nnnnn..DDD -- SVE integer compare scalar count and limit
                           // (predicate-as-counter)
            code = emitInsCodeSve(ins, fmt);
            code |= insEncodeVectorLengthSpecifier(id);                   // l
            code |= insEncodeReg_P<2, 0>(id->idReg1());                   // DDD
            code |= insEncodeReg_R<9, 5>(id->idReg2());                   // nnnnn
            code |= insEncodeReg_R<20, 16>(id->idReg3());                 // mmmmm
            code |= insEncodeElemsize(optGetSveElemsize(id->idInsOpt())); // xx
            dst += emitOutput_Instr(dst, code);
            break;

        case IF_SVE_DZ_1A: // ........xx...... .............DDD -- sve_int_pn_ptrue
            code = emitInsCodeSve(ins, fmt);
            code |= insEncodeReg_P<2, 0>(id->idReg1());                   // DDD
            code |= insEncodeElemsize(optGetSveElemsize(id->idInsOpt())); // xx
            dst += emitOutput_Instr(dst, code);
            break;

        case IF_SVE_EA_1A: // ........xx...... ...iiiiiiiiddddd -- SVE broadcast floating-point immediate (unpredicated)
        case IF_SVE_ED_1A: // ........xx...... ...iiiiiiiiddddd -- SVE integer min/max immediate (unpredicated)
        case IF_SVE_EE_1A: // ........xx...... ...iiiiiiiiddddd -- SVE integer multiply immediate (unpredicated)
        {
            imm  = emitGetInsSC(id);
            code = emitInsCodeSve(ins, fmt);
            code |= insEncodeReg_V<4, 0>(id->idReg1());                   // ddddd
            code |= insEncodeImm8_12_to_5(imm);                           // iiiiiiii
            code |= insEncodeElemsize(optGetSveElemsize(id->idInsOpt())); // xx
            dst += emitOutput_Instr(dst, code);
            break;
        }

        case IF_SVE_FA_3A: // ...........iimmm ....rrnnnnnddddd -- SVE2 complex integer dot product (indexed)
        case IF_SVE_FB_3A: // ...........iimmm ....rrnnnnnddddd -- SVE2 complex integer multiply-add (indexed)
        case IF_SVE_FC_3A: // ...........iimmm ....rrnnnnnddddd -- SVE2 complex saturating multiply-add (indexed)
        {
            const ssize_t imm   = emitGetInsSC(id);
            const ssize_t rot   = (imm & 0b11);
            const ssize_t index = (imm >> 2);
            code                = emitInsCodeSve(ins, fmt);
            code |= insEncodeReg_V<4, 0>(id->idReg1());   // ddddd
            code |= insEncodeReg_V<9, 5>(id->idReg2());   // nnnnn
            code |= insEncodeUimm<11, 10>(rot);           // rr
            code |= insEncodeReg_V<18, 16>(id->idReg3()); // mmm
            code |= insEncodeUimm<20, 19>(index);         // ii
            dst += emitOutput_Instr(dst, code);
            break;
        }

        case IF_SVE_EJ_3A: // ........xx.mmmmm ....rrnnnnnddddd -- SVE2 complex integer dot product
        case IF_SVE_EK_3A: // ........xx.mmmmm ....rrnnnnnddddd -- SVE2 complex integer multiply-add
            code = emitInsCodeSve(ins, fmt);
            code |= insEncodeReg_V<4, 0>(id->idReg1());                   // ddddd
            code |= insEncodeReg_V<9, 5>(id->idReg2());                   // nnnnn
            code |= insEncodeUimm<11, 10>(emitGetInsSC(id));              // rr
            code |= insEncodeReg_V<20, 16>(id->idReg3());                 // mmmmm
            code |= insEncodeElemsize(optGetSveElemsize(id->idInsOpt())); // xx
            dst += emitOutput_Instr(dst, code);
            break;

        case IF_SVE_FA_3B: // ...........immmm ....rrnnnnnddddd -- SVE2 complex integer dot product (indexed)
        case IF_SVE_FB_3B: // ...........immmm ....rrnnnnnddddd -- SVE2 complex integer multiply-add (indexed)
        case IF_SVE_FC_3B: // ...........immmm ....rrnnnnnddddd -- SVE2 complex saturating multiply-add (indexed)
        case IF_SVE_GV_3A: // ...........immmm ....rrnnnnnddddd -- SVE floating-point complex multiply-add (indexed)
        {
            const ssize_t imm   = emitGetInsSC(id);
            const ssize_t rot   = (imm & 0b11);
            const ssize_t index = (imm >> 2);
            code                = emitInsCodeSve(ins, fmt);
            code |= insEncodeReg_V<4, 0>(id->idReg1());   // ddddd
            code |= insEncodeReg_V<9, 5>(id->idReg2());   // nnnnn
            code |= insEncodeReg_V<19, 16>(id->idReg3()); // mmmm
            code |= insEncodeUimm<11, 10>(rot);           // rr

            // index is encoded at bit location 20;
            // left-shift by one bit so we can reuse insEncodeUimm<20, 19> without modifying bit location 19
            code |= insEncodeUimm<20, 19>(index << 1); // i
            dst += emitOutput_Instr(dst, code);
            break;
        }

        case IF_SVE_EB_1A: // ........xx...... ..hiiiiiiiiddddd -- SVE broadcast integer immediate (unpredicated)
        case IF_SVE_EC_1A: // ........xx...... ..hiiiiiiiiddddd -- SVE integer add/subtract immediate (unpredicated)
            imm  = emitGetInsSC(id);
            code = emitInsCodeSve(ins, fmt);
            code |= insEncodeReg_V<4, 0>(id->idReg1());                   // ddddd
            code |= insEncodeElemsize(optGetSveElemsize(id->idInsOpt())); // xx
            code |= insEncodeImm8_12_to_5(imm);                           // iiiiiiii
            code |= (id->idHasShift() ? 0x2000 : 0);                      // h
            dst += emitOutput_Instr(dst, code);
            break;

        case IF_SVE_EB_1B: // ........xx...... ...........ddddd -- SVE broadcast integer immediate (unpredicated)
            // ins is MOV for this encoding, as it is the preferred disassembly, so pass FMOV to emitInsCodeSve
            code = emitInsCodeSve(INS_sve_fmov, fmt);
            code |= insEncodeReg_V<4, 0>(id->idReg1());                   // ddddd
            code |= insEncodeElemsize(optGetSveElemsize(id->idInsOpt())); // xx
            dst += emitOutput_Instr(dst, code);
            break;

        case IF_SVE_DU_3A: // ........xx.mmmmm ......nnnnn.DDDD -- SVE pointer conflict compare
            code = emitInsCodeSve(ins, fmt);
            code |= insEncodeReg_P<3, 0>(id->idReg1());                      // DDDD
            code |= insEncodeReg_R<9, 5>(id->idReg2());                      // nnnnn
            code |= insEncodeReg_R<20, 16>(id->idReg3());                    // mmmmm
            code |= insEncodeSveElemsize(optGetSveElemsize(id->idInsOpt())); // xx
            dst += emitOutput_Instr(dst, code);
            break;

        case IF_SVE_DV_4A: // ........ix.xxxvv ..NNNN.MMMM.DDDD -- SVE broadcast predicate element
            code = emitInsCodeSve(ins, fmt);
            code |= insEncodeReg_P<3, 0>(id->idReg1());                                       // DDDD
            code |= insEncodeReg_P<13, 10>(id->idReg2());                                     // NNNN
            code |= insEncodeReg_P<8, 5>(id->idReg3());                                       // MMMM
            code |= insEncodeReg_R<17, 16>(id->idReg4());                                     // vv
            code |= insEncodeSveElemsize_tszh_tszl_and_imm(id->idInsOpt(), emitGetInsSC(id)); // ix xx
            dst += emitOutput_Instr(dst, code);
            break;

        case IF_SVE_HO_3A: // ................ ...gggnnnnnddddd -- SVE floating-point convert precision
            code = emitInsCodeSve(ins, fmt);
            code |= insEncodeReg_V<4, 0>(id->idReg1());   // ddddd
            code |= insEncodeReg_P<12, 10>(id->idReg2()); // ggg
            code |= insEncodeReg_V<9, 5>(id->idReg3());   // nnnnn
            dst += emitOutput_Instr(dst, code);
            break;

        case IF_SVE_HO_3B: // ................ ...gggnnnnnddddd -- SVE floating-point convert precision
            code = emitInsCodeSve(ins, fmt);
            code |= insEncodeReg_V<4, 0>(id->idReg1());   // ddddd
            code |= insEncodeReg_P<12, 10>(id->idReg2()); // ggg
            code |= insEncodeReg_V<9, 5>(id->idReg3());   // nnnnn
            switch (id->idInsOpt())
            {
                case INS_OPTS_H_TO_S:
                    code |= (1 << 16);
                    break;
                case INS_OPTS_H_TO_D:
                    code |= (1 << 22) | (1 << 16);
                    break;
                case INS_OPTS_S_TO_H:
                    break;
                case INS_OPTS_S_TO_D:
                    code |= (1 << 22) | (3 << 16);
                    break;
                case INS_OPTS_D_TO_H:
                    code |= (1 << 22);
                    break;
                case INS_OPTS_D_TO_S:
                    code |= (1 << 22) | (1 << 17);
                    break;
                default:
                    unreached();
            }
            dst += emitOutput_Instr(dst, code);
            break;

        case IF_SVE_HO_3C: // ................ ...gggnnnnnddddd -- SVE floating-point convert precision
            code = emitInsCodeSve(ins, fmt);
            code |= insEncodeReg_V<4, 0>(id->idReg1());   // ddddd
            code |= insEncodeReg_P<12, 10>(id->idReg2()); // ggg
            code |= insEncodeReg_V<9, 5>(id->idReg3());   // nnnnn
            dst += emitOutput_Instr(dst, code);
            break;

        case IF_SVE_HP_3B: // ................ ...gggnnnnnddddd -- SVE floating-point convert to integer
            code = emitInsCodeSve(ins, fmt);
            code |= insEncodeReg_V<4, 0>(id->idReg1());   // ddddd
            code |= insEncodeReg_P<12, 10>(id->idReg2()); // ggg
            code |= insEncodeReg_V<9, 5>(id->idReg3());   // nnnnn

            switch (id->idInsOpt())
            {
                case INS_OPTS_SCALABLE_H:
                    code |= (1 << 22) | (1 << 17);
                    break;
                case INS_OPTS_H_TO_S:
                    code |= (1 << 22) | (1 << 18);
                    break;
                case INS_OPTS_H_TO_D:
                    code |= (1 << 22) | (3 << 17);
                    break;
                case INS_OPTS_SCALABLE_S:
                    code |= (1 << 23) | (1 << 18);
                    break;
                case INS_OPTS_S_TO_D:
                    code |= (3 << 22) | (1 << 18);
                    break;
                case INS_OPTS_D_TO_S:
                    code |= (3 << 22);
                    break;
                case INS_OPTS_SCALABLE_D:
                    code |= (3 << 22) | (3 << 17);
                    break;
                default:
                    unreached();
                    break;
            }

            dst += emitOutput_Instr(dst, code);
            break;

        case IF_SVE_HS_3A: // ................ ...gggnnnnnddddd -- SVE integer convert to floating-point
            code = emitInsCodeSve(ins, fmt);
            code |= insEncodeReg_V<4, 0>(id->idReg1());   // ddddd
            code |= insEncodeReg_P<12, 10>(id->idReg2()); // ggg
            code |= insEncodeReg_V<9, 5>(id->idReg3());   // nnnnn

            switch (id->idInsOpt())
            {
                case INS_OPTS_SCALABLE_H:
                    code |= (1 << 22) | (1 << 17);
                    break;
                case INS_OPTS_S_TO_H:
                    code |= (1 << 22) | (1 << 18);
                    break;
                case INS_OPTS_SCALABLE_S:
                    code |= (1 << 23) | (1 << 18);
                    break;
                case INS_OPTS_S_TO_D:
                    code |= (1 << 23) | (1 << 22);
                    break;
                case INS_OPTS_D_TO_H:
                    code |= (1 << 22) | (3 << 17);
                    break;
                case INS_OPTS_D_TO_S:
                    code |= (3 << 22) | (1 << 18);
                    break;
                case INS_OPTS_SCALABLE_D:
                    code |= (3 << 22) | (3 << 17);
                    break;
                default:
                    unreached();
                    break;
            }

            dst += emitOutput_Instr(dst, code);
            break;

        case IF_SVE_IH_3A:   // ............iiii ...gggnnnnnttttt -- SVE contiguous load (quadwords, scalar plus
                             // immediate)
        case IF_SVE_IH_3A_A: // ............iiii ...gggnnnnnttttt -- SVE contiguous load (quadwords, scalar plus
                             // immediate)
        case IF_SVE_IH_3A_F: // ............iiii ...gggnnnnnttttt -- SVE contiguous load (quadwords, scalar plus
                             // immediate)
        case IF_SVE_IJ_3A:   // ............iiii ...gggnnnnnttttt -- SVE contiguous load (scalar plus immediate)
        case IF_SVE_IJ_3A_D: // ............iiii ...gggnnnnnttttt -- SVE contiguous load (scalar plus immediate)
        case IF_SVE_IJ_3A_E: // ............iiii ...gggnnnnnttttt -- SVE contiguous load (scalar plus immediate)
        case IF_SVE_IJ_3A_F: // ............iiii ...gggnnnnnttttt -- SVE contiguous load (scalar plus immediate)
        case IF_SVE_IJ_3A_G: // ............iiii ...gggnnnnnttttt -- SVE contiguous load (scalar plus immediate)
        case IF_SVE_IL_3A: // ............iiii ...gggnnnnnttttt -- SVE contiguous non-fault load (scalar plus immediate)
        case IF_SVE_IL_3A_A: // ............iiii ...gggnnnnnttttt -- SVE contiguous non-fault load (scalar plus
                             // immediate)
        case IF_SVE_IL_3A_B: // ............iiii ...gggnnnnnttttt -- SVE contiguous non-fault load (scalar plus
                             // immediate)
        case IF_SVE_IL_3A_C: // ............iiii ...gggnnnnnttttt -- SVE contiguous non-fault load (scalar plus
                             // immediate)
        case IF_SVE_IM_3A:   // ............iiii ...gggnnnnnttttt -- SVE contiguous non-temporal load (scalar plus
                             // immediate)
        case IF_SVE_IO_3A:   // ............iiii ...gggnnnnnttttt -- SVE load and broadcast quadword (scalar plus
                             // immediate)
        case IF_SVE_IQ_3A: // ............iiii ...gggnnnnnttttt -- SVE load multiple structures (quadwords, scalar plus
                           // immediate)
        case IF_SVE_IS_3A: // ............iiii ...gggnnnnnttttt -- SVE load multiple structures (scalar plus immediate)
        case IF_SVE_JE_3A: // ............iiii ...gggnnnnnttttt -- SVE store multiple structures (quadwords, scalar plus
                           // immediate)
        case IF_SVE_JM_3A: // ............iiii ...gggnnnnnttttt -- SVE contiguous non-temporal store (scalar plus
                           // immediate)
        case IF_SVE_JN_3C: // ............iiii ...gggnnnnnttttt -- SVE contiguous store (scalar plus immediate)
        case IF_SVE_JN_3C_D: // ............iiii ...gggnnnnnttttt -- SVE contiguous store (scalar plus immediate)
        case IF_SVE_JO_3A: // ............iiii ...gggnnnnnttttt -- SVE store multiple structures (scalar plus immediate)
            imm  = emitGetInsSC(id);
            code = emitInsCodeSve(ins, fmt);
            code |= insEncodeReg_V<4, 0>(id->idReg1());   // ttttt
            code |= insEncodeReg_R<9, 5>(id->idReg3());   // nnnnn
            code |= insEncodeReg_P<12, 10>(id->idReg2()); // ggg

            switch (ins)
            {
                case INS_sve_ld2b:
                case INS_sve_ld2h:
                case INS_sve_ld2w:
                case INS_sve_ld2d:
                case INS_sve_ld2q:
                case INS_sve_st2b:
                case INS_sve_st2h:
                case INS_sve_st2w:
                case INS_sve_st2d:
                case INS_sve_st2q:
                    code |= insEncodeSimm_MultipleOf<19, 16, 2>(imm); // iiii
                    break;

                case INS_sve_ld3b:
                case INS_sve_ld3h:
                case INS_sve_ld3w:
                case INS_sve_ld3d:
                case INS_sve_ld3q:
                case INS_sve_st3b:
                case INS_sve_st3h:
                case INS_sve_st3w:
                case INS_sve_st3d:
                case INS_sve_st3q:
                    code |= insEncodeSimm_MultipleOf<19, 16, 3>(imm); // iiii
                    break;

                case INS_sve_ld4b:
                case INS_sve_ld4h:
                case INS_sve_ld4w:
                case INS_sve_ld4d:
                case INS_sve_ld4q:
                case INS_sve_st4b:
                case INS_sve_st4h:
                case INS_sve_st4w:
                case INS_sve_st4d:
                case INS_sve_st4q:
                    code |= insEncodeSimm_MultipleOf<19, 16, 4>(imm); // iiii
                    break;

                case INS_sve_ld1rqb:
                case INS_sve_ld1rqd:
                case INS_sve_ld1rqh:
                case INS_sve_ld1rqw:
                    code |= insEncodeSimm_MultipleOf<19, 16, 16>(imm); // iiii
                    break;

                case INS_sve_ld1rob:
                case INS_sve_ld1rod:
                case INS_sve_ld1roh:
                case INS_sve_ld1row:
                    code |= insEncodeSimm_MultipleOf<19, 16, 32>(imm); // iiii
                    break;

                default:
                    code |= insEncodeSimm<19, 16>(imm); // iiii
                    break;
            }

            if (canEncodeSveElemsize_dtype(ins))
            {
                if (ins == INS_sve_ld1w)
                {
                    code = insEncodeSveElemsize_dtype_ld1w(ins, fmt, optGetSveElemsize(id->idInsOpt()), code);
                }
                else
                {
                    code = insEncodeSveElemsize_dtype(ins, optGetSveElemsize(id->idInsOpt()), code);
                }
            }

            dst += emitOutput_Instr(dst, code);
            break;

        case IF_SVE_JD_4A: // .........xxmmmmm ...gggnnnnnttttt -- SVE contiguous store (scalar plus scalar)
            code = emitInsCodeSve(ins, fmt);
            code |= insEncodeReg_V<4, 0>(id->idReg1());                               // ttttt
            code |= insEncodeReg_P<12, 10>(id->idReg2());                             // ggg
            code |= insEncodeReg_R<9, 5>(id->idReg3());                               // nnnnn
            code |= insEncodeReg_R<20, 16>(id->idReg4());                             // mmmmm
            code |= insEncodeSveElemsize_22_to_21(optGetSveElemsize(id->idInsOpt())); // xx
            dst += emitOutput_Instr(dst, code);
            break;

        case IF_SVE_JD_4B: // ..........xmmmmm ...gggnnnnnttttt -- SVE contiguous store (scalar plus scalar)
            code = emitInsCodeSve(ins, fmt);
            code |= insEncodeReg_V<4, 0>(id->idReg1());                            // ttttt
            code |= insEncodeReg_P<12, 10>(id->idReg2());                          // ggg
            code |= insEncodeReg_R<9, 5>(id->idReg3());                            // nnnnn
            code |= insEncodeReg_R<20, 16>(id->idReg4());                          // mmmmm
            code |= insEncodeSveElemsize_sz_21(optGetSveElemsize(id->idInsOpt())); // x
            dst += emitOutput_Instr(dst, code);
            break;

        case IF_SVE_JJ_4A:   // ...........mmmmm .h.gggnnnnnttttt -- SVE 64-bit scatter store (scalar plus 64-bit scaled
                             // offsets)
        case IF_SVE_JJ_4A_B: // ...........mmmmm .h.gggnnnnnttttt -- SVE 64-bit scatter store (scalar plus 64-bit scaled
                             //                     // offsets)
        case IF_SVE_JJ_4A_C: // ...........mmmmm .h.gggnnnnnttttt -- SVE 64-bit scatter store (scalar plus 64-bit scaled
                             // offsets)
        case IF_SVE_JJ_4A_D: // ...........mmmmm .h.gggnnnnnttttt -- SVE 64-bit scatter store (scalar plus 64-bit scaled
                             // offsets)
        case IF_SVE_JK_4A: // ...........mmmmm .h.gggnnnnnttttt -- SVE 64-bit scatter store (scalar plus 64-bit unscaled
                           // offsets)
        case IF_SVE_JK_4A_B: // ...........mmmmm .h.gggnnnnnttttt -- SVE 64-bit scatter store (scalar plus 64-bit
                             // unscaled offsets)
            code = emitInsCodeSve(ins, fmt);
            code |= insEncodeReg_V<4, 0>(id->idReg1());   // ttttt
            code |= insEncodeReg_P<12, 10>(id->idReg2()); // ggg
            code |= insEncodeReg_R<9, 5>(id->idReg3());   // nnnnn
            code |= insEncodeReg_V<20, 16>(id->idReg4()); // mmmmm

            switch (id->idInsOpt())
            {
                case INS_OPTS_SCALABLE_S_SXTW:
                case INS_OPTS_SCALABLE_D_SXTW:
                    code |= (1 << 14); // h
                    break;

                default:
                    break;
            }

            dst += emitOutput_Instr(dst, code);
            break;

        case IF_SVE_JN_3A: // .........xx.iiii ...gggnnnnnttttt -- SVE contiguous store (scalar plus immediate)
            imm  = emitGetInsSC(id);
            code = emitInsCodeSve(ins, fmt);
            code |= insEncodeReg_V<4, 0>(id->idReg1());                               // ttttt
            code |= insEncodeReg_P<12, 10>(id->idReg2());                             // ggg
            code |= insEncodeReg_R<9, 5>(id->idReg3());                               // nnnnn
            code |= insEncodeSimm<19, 16>(imm);                                       // iiii
            code |= insEncodeSveElemsize_22_to_21(optGetSveElemsize(id->idInsOpt())); // xx
            dst += emitOutput_Instr(dst, code);
            break;

        case IF_SVE_JN_3B: // ..........x.iiii ...gggnnnnnttttt -- SVE contiguous store (scalar plus immediate)
            imm  = emitGetInsSC(id);
            code = emitInsCodeSve(ins, fmt);
            code |= insEncodeReg_V<4, 0>(id->idReg1());                            // ttttt
            code |= insEncodeReg_P<12, 10>(id->idReg2());                          // ggg
            code |= insEncodeReg_R<9, 5>(id->idReg3());                            // nnnnn
            code |= insEncodeSimm<19, 16>(imm);                                    // iiii
            code |= insEncodeSveElemsize_sz_21(optGetSveElemsize(id->idInsOpt())); // x
            dst += emitOutput_Instr(dst, code);
            break;

        case IF_SVE_HW_4A:   // .........h.mmmmm ...gggnnnnnttttt -- SVE 32-bit gather load (scalar plus 32-bit unscaled
                             // offsets)
        case IF_SVE_HW_4A_A: // .........h.mmmmm ...gggnnnnnttttt -- SVE 32-bit gather load (scalar plus 32-bit unscaled
                             // offsets)
        case IF_SVE_HW_4A_B: // .........h.mmmmm ...gggnnnnnttttt -- SVE 32-bit gather load (scalar plus 32-bit unscaled
                             // offsets)
        case IF_SVE_HW_4A_C: // .........h.mmmmm ...gggnnnnnttttt -- SVE 32-bit gather load (scalar plus 32-bit unscaled
                             // offsets)
        case IF_SVE_IU_4A:   // .........h.mmmmm ...gggnnnnnttttt -- SVE 64-bit gather load (scalar plus 32-bit unpacked
                             // scaled offsets)
        case IF_SVE_IU_4A_A: // .........h.mmmmm ...gggnnnnnttttt -- SVE 64-bit gather load (scalar plus 32-bit unpacked
                             // scaled offsets)
        case IF_SVE_IU_4A_C: // .........h.mmmmm ...gggnnnnnttttt -- SVE 64-bit gather load (scalar plus 32-bit unpacked
                             // scaled offsets)
            code = emitInsCodeSve(ins, fmt);
            code |= insEncodeReg_V<4, 0>(id->idReg1());   // ttttt
            code |= insEncodeReg_P<12, 10>(id->idReg2()); // ggg
            code |= insEncodeReg_R<9, 5>(id->idReg3());   // nnnnn
            code |= insEncodeReg_V<20, 16>(id->idReg4()); // mmmmm

            switch (id->idInsOpt())
            {
                case INS_OPTS_SCALABLE_S_SXTW:
                case INS_OPTS_SCALABLE_D_SXTW:
                    code |= (1 << 22); // h
                    break;

                default:
                    break;
            }

            dst += emitOutput_Instr(dst, code);
            break;

        case IF_SVE_HW_4B:   // ...........mmmmm ...gggnnnnnttttt -- SVE 32-bit gather load (scalar plus 32-bit unscaled
                             // offsets)
        case IF_SVE_HW_4B_D: // ...........mmmmm ...gggnnnnnttttt -- SVE 32-bit gather load (scalar plus 32-bit unscaled
                             // offsets)
            code = emitInsCodeSve(ins, fmt);
            code |= insEncodeReg_V<4, 0>(id->idReg1());   // ttttt
            code |= insEncodeReg_P<12, 10>(id->idReg2()); // ggg
            code |= insEncodeReg_R<9, 5>(id->idReg3());   // nnnnn
            code |= insEncodeReg_V<20, 16>(id->idReg4()); // mmmmm
            dst += emitOutput_Instr(dst, code);
            break;

        case IF_SVE_IF_4A:   // ...........mmmmm ...gggnnnnnttttt -- SVE2 32-bit gather non-temporal load (vector plus
                             // scalar)
        case IF_SVE_IF_4A_A: // ...........mmmmm ...gggnnnnnttttt -- SVE2 32-bit gather non-temporal load (vector plus
                             // scalar)
        case IF_SVE_IW_4A:   // ...........mmmmm ...gggnnnnnttttt -- SVE2 128-bit gather load (vector plus scalar)
        case IF_SVE_IX_4A:   // ...........mmmmm ...gggnnnnnttttt -- SVE2 64-bit gather non-temporal load (vector plus
                             // scalar)
        case IF_SVE_IY_4A:   // ...........mmmmm ...gggnnnnnttttt -- SVE2 128-bit scatter store (vector plus scalar)
        case IF_SVE_IZ_4A:   // ...........mmmmm ...gggnnnnnttttt -- SVE2 32-bit scatter non-temporal store (vector plus
                             // scalar)
        case IF_SVE_IZ_4A_A: // ...........mmmmm ...gggnnnnnttttt -- SVE2 32-bit scatter non-temporal store (vector plus
                             // scalar)
        case IF_SVE_JA_4A:   // ...........mmmmm ...gggnnnnnttttt -- SVE2 64-bit scatter non-temporal store (vector plus
                             // scalar)
            code = emitInsCodeSve(ins, fmt);
            code |= insEncodeReg_V<4, 0>(id->idReg1());   // ttttt
            code |= insEncodeReg_P<12, 10>(id->idReg2()); // ggg
            code |= insEncodeReg_V<9, 5>(id->idReg3());   // nnnnn
            code |= insEncodeReg_R<20, 16>(id->idReg4()); // mmmmm
            dst += emitOutput_Instr(dst, code);
            break;

        case IF_SVE_IG_4A_D: // ...........mmmmm ...gggnnnnnttttt -- SVE contiguous first-fault load (scalar plus
                             // scalar)
        case IF_SVE_IG_4A_E: // ...........mmmmm ...gggnnnnnttttt -- SVE contiguous first-fault load (scalar plus
                             // scalar)
        case IF_SVE_IG_4A_F: // ...........mmmmm ...gggnnnnnttttt -- SVE contiguous first-fault load (scalar plus
                             // scalar)
        case IF_SVE_IG_4A_G: // ...........mmmmm ...gggnnnnnttttt -- SVE contiguous first-fault load (scalar plus
                             // scalar)
        case IF_SVE_II_4A_H: // ...........mmmmm ...gggnnnnnttttt -- SVE contiguous load (quadwords, scalar plus scalar)
        case IF_SVE_IK_4A_F: // ...........mmmmm ...gggnnnnnttttt -- SVE contiguous load (scalar plus scalar)
        case IF_SVE_IK_4A_G: // ...........mmmmm ...gggnnnnnttttt -- SVE contiguous load (scalar plus scalar)
        case IF_SVE_IK_4A_H: // ...........mmmmm ...gggnnnnnttttt -- SVE contiguous load (scalar plus scalar)
        case IF_SVE_IK_4A_I: // ...........mmmmm ...gggnnnnnttttt -- SVE contiguous load (scalar plus scalar)
            code = emitInsCodeSve(ins, fmt);
            code |= insEncodeReg_V<4, 0>(id->idReg1());   // ttttt
            code |= insEncodeReg_P<12, 10>(id->idReg2()); // ggg
            code |= insEncodeReg_R<9, 5>(id->idReg3());   // nnnnn
            code |= insEncodeReg_R<20, 16>(id->idReg4()); // mmmmm

            if (canEncodeSveElemsize_dtype(ins))
            {
                if (ins == INS_sve_ld1w)
                {
                    code = insEncodeSveElemsize_dtype_ld1w(ins, fmt, optGetSveElemsize(id->idInsOpt()), code);
                }
                else
                {
                    code = insEncodeSveElemsize_dtype(ins, optGetSveElemsize(id->idInsOpt()), code);
                }
            }

            dst += emitOutput_Instr(dst, code);
            break;

        case IF_SVE_IG_4A: // ...........mmmmm ...gggnnnnnttttt -- SVE contiguous first-fault load (scalar plus scalar)
        case IF_SVE_II_4A: // ...........mmmmm ...gggnnnnnttttt -- SVE contiguous load (quadwords, scalar plus scalar)
        case IF_SVE_II_4A_B: // ...........mmmmm ...gggnnnnnttttt -- SVE contiguous load (quadwords, scalar plus scalar)
        case IF_SVE_IK_4A:   // ...........mmmmm ...gggnnnnnttttt -- SVE contiguous load (scalar plus scalar)
        case IF_SVE_IN_4A: // ...........mmmmm ...gggnnnnnttttt -- SVE contiguous non-temporal load (scalar plus scalar)
        case IF_SVE_IP_4A: // ...........mmmmm ...gggnnnnnttttt -- SVE load and broadcast quadword (scalar plus scalar)
        case IF_SVE_IR_4A: // ...........mmmmm ...gggnnnnnttttt -- SVE load multiple structures (quadwords, scalar plus
                           // scalar)
        case IF_SVE_IT_4A: // ...........mmmmm ...gggnnnnnttttt -- SVE load multiple structures (scalar plus scalar)
        case IF_SVE_JB_4A: // ...........mmmmm ...gggnnnnnttttt -- SVE contiguous non-temporal store (scalar plus
                           // scalar)
        case IF_SVE_JC_4A: // ...........mmmmm ...gggnnnnnttttt -- SVE store multiple structures (scalar plus scalar)
        case IF_SVE_JD_4C: // ...........mmmmm ...gggnnnnnttttt -- SVE contiguous store (scalar plus scalar)
        case IF_SVE_JD_4C_A: // ...........mmmmm ...gggnnnnnttttt -- SVE contiguous store (scalar plus scalar)
        case IF_SVE_JF_4A: // ...........mmmmm ...gggnnnnnttttt -- SVE store multiple structures (quadwords, scalar plus
                           // scalar)
            code = emitInsCodeSve(ins, fmt);
            code |= insEncodeReg_V<4, 0>(id->idReg1());   // ttttt
            code |= insEncodeReg_P<12, 10>(id->idReg2()); // ggg
            code |= insEncodeReg_R<9, 5>(id->idReg3());   // nnnnn
            code |= insEncodeReg_R<20, 16>(id->idReg4()); // mmmmm
            dst += emitOutput_Instr(dst, code);
            break;

        case IF_SVE_IU_4B:   // ...........mmmmm ...gggnnnnnttttt -- SVE 64-bit gather load (scalar plus 32-bit unpacked
                             // scaled offsets)
        case IF_SVE_IU_4B_B: // ...........mmmmm ...gggnnnnnttttt -- SVE 64-bit gather load (scalar plus 32-bit unpacked
                             // scaled offsets)
        case IF_SVE_IU_4B_D: // ...........mmmmm ...gggnnnnnttttt -- SVE 64-bit gather load (scalar plus 32-bit unpacked
                             // scaled offsets)
        case IF_SVE_JJ_4B:   // ...........mmmmm ...gggnnnnnttttt -- SVE 64-bit scatter store (scalar plus 64-bit scaled
                             // offsets)
        case IF_SVE_JJ_4B_C: // ...........mmmmm ...gggnnnnnttttt -- SVE 64-bit scatter store (scalar plus 64-bit scaled
                             // offsets)
        case IF_SVE_JJ_4B_E: // ...........mmmmm ...gggnnnnnttttt -- SVE 64-bit scatter store (scalar plus 64-bit scaled
                             // offsets)
        case IF_SVE_JK_4B: // ...........mmmmm ...gggnnnnnttttt -- SVE 64-bit scatter store (scalar plus 64-bit unscaled
                           // offsets)
            code = emitInsCodeSve(ins, fmt);
            code |= insEncodeReg_V<4, 0>(id->idReg1());   // ttttt
            code |= insEncodeReg_P<12, 10>(id->idReg2()); // ggg
            code |= insEncodeReg_R<9, 5>(id->idReg3());   // nnnnn
            code |= insEncodeReg_V<20, 16>(id->idReg4()); // mmmmm
            dst += emitOutput_Instr(dst, code);
            break;

        case IF_SVE_GP_3A: // ........xx.....r ...gggmmmmmddddd -- SVE floating-point complex add (predicated)
            imm  = emitGetInsSC(id);
            code = emitInsCodeSve(ins, fmt);
            code |= insEncodeReg_V<4, 0>(id->idReg1());                      // ddddd
            code |= insEncodeReg_P<12, 10>(id->idReg2());                    // ggg
            code |= insEncodeReg_V<9, 5>(id->idReg3());                      // mmmmm
            code |= insEncodeSveImm90_or_270_rot(imm);                       // r
            code |= insEncodeSveElemsize(optGetSveElemsize(id->idInsOpt())); // xx
            dst += emitOutput_Instr(dst, code);
            break;

        case IF_SVE_GT_4A: // ........xx.mmmmm .rrgggnnnnnddddd -- SVE floating-point complex multiply-add (predicated)
            imm  = emitGetInsSC(id);
            code = emitInsCodeSve(ins, fmt);
            code |= insEncodeReg_V<4, 0>(id->idReg1());                      // ddddd
            code |= insEncodeReg_P<12, 10>(id->idReg2());                    // ggg
            code |= insEncodeReg_V<9, 5>(id->idReg3());                      // nnnnn
            code |= insEncodeReg_V<20, 16>(id->idReg4());                    // mmmmm
            code |= insEncodeSveImm0_to_270_rot(imm);                        // rr
            code |= insEncodeSveElemsize(optGetSveElemsize(id->idInsOpt())); // xx
            dst += emitOutput_Instr(dst, code);
            break;

        case IF_SVE_HI_3A: // ........xx...... ...gggnnnnn.DDDD -- SVE floating-point compare with zero
            code = emitInsCodeSve(ins, fmt);
            code |= insEncodeReg_P<3, 0>(id->idReg1());                      // DDDD
            code |= insEncodeReg_P<12, 10>(id->idReg2());                    // ggg
            code |= insEncodeReg_V<9, 5>(id->idReg3());                      // nnnnn
            code |= insEncodeSveElemsize(optGetSveElemsize(id->idInsOpt())); // xx
            dst += emitOutput_Instr(dst, code);
            break;

        case IF_SVE_HM_2A: // ........xx...... ...ggg....iddddd -- SVE floating-point arithmetic with immediate
                           // (predicated)
        {
            imm  = emitGetInsSC(id);
            code = emitInsCodeSve(ins, fmt);
            code |= insEncodeReg_V<4, 0>(id->idReg1());                      // ddddd
            code |= insEncodeReg_P<12, 10>(id->idReg2());                    // ggg
            code |= insEncodeSveSmallFloatImm(imm);                          // i
            code |= insEncodeSveElemsize(optGetSveElemsize(id->idInsOpt())); // xx
            dst += emitOutput_Instr(dst, code);
        }
        break;

        case IF_SVE_HN_2A: // ........xx...iii ......mmmmmddddd -- SVE floating-point trig multiply-add coefficient
            imm  = emitGetInsSC(id);
            code = emitInsCodeSve(ins, fmt);
            code |= insEncodeReg_V<4, 0>(id->idReg1());                      // ddddd
            code |= insEncodeReg_V<9, 5>(id->idReg2());                      // mmmmm
            code |= insEncodeUimm<18, 16>(imm);                              // iii
            code |= insEncodeSveElemsize(optGetSveElemsize(id->idInsOpt())); // xx
            dst += emitOutput_Instr(dst, code);
            break;

        case IF_SVE_HP_3A: // .............xx. ...gggnnnnnddddd -- SVE floating-point convert to integer
            code = emitInsCodeSve(ins, fmt);
            code |= insEncodeReg_V<4, 0>(id->idReg1());                               // ddddd
            code |= insEncodeReg_P<12, 10>(id->idReg2());                             // ggg
            code |= insEncodeReg_V<9, 5>(id->idReg3());                               // nnnnn
            code |= insEncodeSveElemsize_18_to_17(optGetSveElemsize(id->idInsOpt())); // xx
            dst += emitOutput_Instr(dst, code);
            break;

        case IF_SVE_HU_4B: // ...........mmmmm ...gggnnnnnddddd -- SVE floating-point multiply-accumulate writing addend
            code = emitInsCodeSve(ins, fmt);
            code |= insEncodeReg_V<4, 0>(id->idReg1());   // ddddd
            code |= insEncodeReg_P<12, 10>(id->idReg2()); // ggg
            code |= insEncodeReg_V<9, 5>(id->idReg3());   // nnnnn
            code |= insEncodeReg_V<20, 16>(id->idReg4()); // mmmmm
            dst += emitOutput_Instr(dst, code);
            break;

        case IF_SVE_HV_4A: // ........xx.aaaaa ...gggmmmmmddddd -- SVE floating-point multiply-accumulate writing
                           // multiplicand
            code = emitInsCodeSve(ins, fmt);
            code |= insEncodeReg_V<4, 0>(id->idReg1());                      // ddddd
            code |= insEncodeReg_P<12, 10>(id->idReg2());                    // ggg
            code |= insEncodeReg_V<9, 5>(id->idReg3());                      // mmmmm
            code |= insEncodeReg_V<20, 16>(id->idReg4());                    // aaaaa
            code |= insEncodeSveElemsize(optGetSveElemsize(id->idInsOpt())); // xx
            dst += emitOutput_Instr(dst, code);
            break;

        case IF_SVE_ID_2A: // ..........iiiiii ...iiinnnnn.TTTT -- SVE load predicate register
        case IF_SVE_JG_2A: // ..........iiiiii ...iiinnnnn.TTTT -- SVE store predicate register
            imm  = emitGetInsSC(id);
            code = emitInsCodeSve(ins, fmt);
            code |= insEncodeReg_P<3, 0>(id->idReg1());           // TTTT
            code |= insEncodeReg_R<9, 5>(id->idReg2());           // nnnnn
            code |= insEncodeSimm9h9l_21_to_16_and_12_to_10(imm); // iii
                                                                  // iiiiii
            dst += emitOutput_Instr(dst, code);
            break;

        case IF_SVE_IE_2A: // ..........iiiiii ...iiinnnnnttttt -- SVE load vector register
        case IF_SVE_JH_2A: // ..........iiiiii ...iiinnnnnttttt -- SVE store vector register
            imm  = emitGetInsSC(id);
            code = emitInsCodeSve(ins, fmt);
            code |= insEncodeReg_V<4, 0>(id->idReg1());           // ttttt
            code |= insEncodeReg_R<9, 5>(id->idReg2());           // nnnnn
            code |= insEncodeSimm9h9l_21_to_16_and_12_to_10(imm); // iii
                                                                  // iiiiii
            dst += emitOutput_Instr(dst, code);
            break;

        case IF_SVE_GG_3A:   // ........ii.mmmmm ......nnnnnddddd -- SVE2 lookup table with 2-bit indices and 16-bit
                             // element size
        case IF_SVE_GH_3B:   // ........ii.mmmmm ......nnnnnddddd -- SVE2 lookup table with 4-bit indices and 16-bit
                             // element size
        case IF_SVE_GH_3B_B: // ........ii.mmmmm ......nnnnnddddd -- SVE2 lookup table with 4-bit indices and 16-bit
                             // element size
            imm  = emitGetInsSC(id);
            code = emitInsCodeSve(ins, fmt);
            code |= insEncodeReg_V<4, 0>(id->idReg1());   // ddddd
            code |= insEncodeReg_V<9, 5>(id->idReg2());   // nnnnn
            code |= insEncodeReg_V<20, 16>(id->idReg3()); // mmmmm
            code |= insEncodeUimm<23, 22>(imm);           // ii
            dst += emitOutput_Instr(dst, code);
            break;

        case IF_SVE_GG_3B: // ........ii.mmmmm ...i..nnnnnddddd -- SVE2 lookup table with 2-bit indices and 16-bit
                           // element size
            imm  = emitGetInsSC(id);
            code = emitInsCodeSve(ins, fmt);
            code |= insEncodeReg_V<4, 0>(id->idReg1());     // ddddd
            code |= insEncodeReg_V<9, 5>(id->idReg2());     // nnnnn
            code |= insEncodeReg_V<20, 16>(id->idReg3());   // mmmmm
            code |= insEncodeUimm3h3l_23_to_22_and_12(imm); // ii
                                                            // i
            dst += emitOutput_Instr(dst, code);
            break;

        case IF_SVE_GH_3A: // ........i..mmmmm ......nnnnnddddd -- SVE2 lookup table with 4-bit indices and 16-bit
                           // element size
            imm  = emitGetInsSC(id);
            code = emitInsCodeSve(ins, fmt);
            code |= insEncodeReg_V<4, 0>(id->idReg1());   // ddddd
            code |= insEncodeReg_V<9, 5>(id->idReg2());   // nnnnn
            code |= insEncodeReg_V<20, 16>(id->idReg3()); // mmmmm
            code |= insEncodeUimm<23, 23>(imm);           // i
            dst += emitOutput_Instr(dst, code);
            break;

        case IF_SVE_HY_3A: // .........h.mmmmm ...gggnnnnn.oooo -- SVE 32-bit gather prefetch (scalar plus 32-bit scaled
                           // offsets)
        case IF_SVE_HY_3A_A: // .........h.mmmmm ...gggnnnnn.oooo -- SVE 32-bit gather prefetch (scalar plus 32-bit
                             // scaled offsets)
            code = emitInsCodeSve(ins, fmt);
            code |= insEncodeReg_P<12, 10>(id->idReg1()); // ggg
            code |= insEncodeReg_R<9, 5>(id->idReg2());   // nnnnn
            code |= insEncodeReg_V<20, 16>(id->idReg3()); // mmmmm
            code |= id->idSvePrfop();                     // oooo

            switch (id->idInsOpt())
            {
                case INS_OPTS_SCALABLE_S_SXTW:
                case INS_OPTS_SCALABLE_D_SXTW:
                    code |= (1 << 22); // h
                    break;

                default:
                    break;
            }

            dst += emitOutput_Instr(dst, code);
            break;

        case IF_SVE_HY_3B: // ...........mmmmm ...gggnnnnn.oooo -- SVE 32-bit gather prefetch (scalar plus 32-bit scaled
                           // offsets)
            code = emitInsCodeSve(ins, fmt);
            code |= insEncodeReg_P<12, 10>(id->idReg1()); // ggg
            code |= insEncodeReg_R<9, 5>(id->idReg2());   // nnnnn
            code |= insEncodeReg_V<20, 16>(id->idReg3()); // mmmmm
            code |= id->idSvePrfop();                     // oooo
            dst += emitOutput_Instr(dst, code);
            break;

        case IF_SVE_IB_3A: // ...........mmmmm ...gggnnnnn.oooo -- SVE contiguous prefetch (scalar plus scalar)
            code = emitInsCodeSve(ins, fmt);
            code |= insEncodeReg_P<12, 10>(id->idReg1()); // ggg
            code |= insEncodeReg_R<9, 5>(id->idReg2());   // nnnnn
            code |= insEncodeReg_R<20, 16>(id->idReg3()); // mmmmm
            code |= id->idSvePrfop();                     // oooo
            dst += emitOutput_Instr(dst, code);
            break;

        case IF_SVE_HZ_2A_B: // ...........iiiii ...gggnnnnn.oooo -- SVE 32-bit gather prefetch (vector plus immediate)
            imm  = emitGetInsSC(id);
            code = emitInsCodeSve(ins, fmt);
            code |= insEncodeReg_P<12, 10>(id->idReg1()); // ggg
            code |= insEncodeReg_V<9, 5>(id->idReg2());   // nnnnn
            code |= id->idSvePrfop();                     // oooo

            if (id->idInsOpt() == INS_OPTS_SCALABLE_D)
            {
                code |= (1 << 30); // set bit '30' to make it a double-word
            }

            switch (ins)
            {
                case INS_sve_prfh:
                    code |= insEncodeUimm_MultipleOf<20, 16, 2>(imm); // iiiii
                    break;

                case INS_sve_prfw:
                    code |= insEncodeUimm_MultipleOf<20, 16, 4>(imm); // iiiii
                    break;

                case INS_sve_prfd:
                    code |= insEncodeUimm_MultipleOf<20, 16, 8>(imm); // iiiii
                    break;

                default:
                    assert(ins == INS_sve_prfb);
            }
            dst += emitOutput_Instr(dst, code);
            break;

        case IF_SVE_HX_3A_B: // ...........iiiii ...gggnnnnnttttt -- SVE 32-bit gather load (vector plus immediate)
            imm  = emitGetInsSC(id);
            code = emitInsCodeSve(ins, fmt);
            code |= insEncodeReg_V<4, 0>(id->idReg1());   // ttttt
            code |= insEncodeReg_P<12, 10>(id->idReg2()); // ggg
            code |= insEncodeReg_V<9, 5>(id->idReg3());   // nnnnn
            code |= insEncodeUimm<20, 16>(imm);           // iiiii
            code |= insEncodeSveElemsize_30_or_21(fmt, optGetSveElemsize(id->idInsOpt()));
            dst += emitOutput_Instr(dst, code);
            break;

        case IF_SVE_HX_3A_E: // ...........iiiii ...gggnnnnnttttt -- SVE 32-bit gather load (vector plus immediate)
        case IF_SVE_IV_3A:   // ...........iiiii ...gggnnnnnttttt -- SVE 64-bit gather load (vector plus immediate)
            imm  = emitGetInsSC(id);
            code = emitInsCodeSve(ins, fmt);
            code |= insEncodeReg_V<4, 0>(id->idReg1());   // ttttt
            code |= insEncodeReg_P<12, 10>(id->idReg2()); // ggg
            code |= insEncodeReg_V<9, 5>(id->idReg3());   // nnnnn
            code |= insEncodeSveElemsize_30_or_21(fmt, optGetSveElemsize(id->idInsOpt()));

            switch (ins)
            {
                case INS_sve_ld1d:
                case INS_sve_ldff1d:
                    code |= insEncodeUimm_MultipleOf<20, 16, 8>(imm); // iiiii
                    break;

                case INS_sve_ld1w:
                case INS_sve_ld1sw:
                case INS_sve_ldff1w:
                case INS_sve_ldff1sw:
                    code |= insEncodeUimm_MultipleOf<20, 16, 4>(imm); // iiiii
                    break;

                default:
                    code |= insEncodeUimm_MultipleOf<20, 16, 2>(imm); // iiiii
                    break;
            }

            dst += emitOutput_Instr(dst, code);
            break;

        case IF_SVE_JL_3A: // ...........iiiii ...gggnnnnnttttt -- SVE 64-bit scatter store (vector plus immediate)
            imm  = emitGetInsSC(id);
            code = emitInsCodeSve(ins, fmt);
            code |= insEncodeReg_V<4, 0>(id->idReg1());       // ttttt
            code |= insEncodeReg_P<12, 10>(id->idReg2());     // ggg
            code |= insEncodeReg_V<9, 5>(id->idReg3());       // nnnnn
            code |= insEncodeUimm_MultipleOf<20, 16, 8>(imm); // iiiii
            dst += emitOutput_Instr(dst, code);
            break;

        case IF_SVE_JI_3A_A: // ...........iiiii ...gggnnnnnttttt -- SVE 32-bit scatter store (vector plus immediate)
            imm  = emitGetInsSC(id);
            code = emitInsCodeSve(ins, fmt);
            code |= insEncodeReg_V<4, 0>(id->idReg1());   // ttttt
            code |= insEncodeReg_P<12, 10>(id->idReg2()); // ggg
            code |= insEncodeReg_V<9, 5>(id->idReg3());   // nnnnn
            code |= insEncodeSveElemsize_30_or_21(fmt, optGetSveElemsize(id->idInsOpt()));

            switch (ins)
            {
                case INS_sve_st1h:
                    code |= insEncodeUimm_MultipleOf<20, 16, 2>(imm); // iiiii
                    break;

                case INS_sve_st1w:
                    code |= insEncodeUimm_MultipleOf<20, 16, 4>(imm); // iiiii
                    break;

                default:
                    assert(ins == INS_sve_st1b);
                    code |= insEncodeUimm<20, 16>(imm); // iiiii
                    break;
            }

            dst += emitOutput_Instr(dst, code);
            break;

        case IF_SVE_IA_2A: // ..........iiiiii ...gggnnnnn.oooo -- SVE contiguous prefetch (scalar plus immediate)
            imm  = emitGetInsSC(id);
            code = emitInsCodeSve(ins, fmt);
            code |= insEncodeReg_P<12, 10>(id->idReg1()); // ggg
            code |= insEncodeReg_R<9, 5>(id->idReg2());   // nnnnn
            code |= id->idSvePrfop();                     // oooo
            code |= insEncodeSimm<21, 16>(imm);           // iiiiii
            dst += emitOutput_Instr(dst, code);
            break;

        case IF_SVE_IC_3A: // ..........iiiiii ...gggnnnnnttttt -- SVE load and broadcast element
            imm  = emitGetInsSC(id);
            code = emitInsCodeSve(ins, fmt);
            code |= insEncodeReg_V<4, 0>(id->idReg1());   // ttttt
            code |= insEncodeReg_P<12, 10>(id->idReg2()); // ggg
            code |= insEncodeReg_R<9, 5>(id->idReg3());   // nnnnn

            switch (ins)
            {
                case INS_sve_ld1rd:
                    code |= insEncodeUimm_MultipleOf<21, 16, 8>(imm); // iiiiii
                    break;

                default:
                    assert(ins == INS_sve_ld1rsw);
                    code |= insEncodeUimm_MultipleOf<21, 16, 4>(imm); // iiiiii
                    break;
            }

            dst += emitOutput_Instr(dst, code);
            break;

        case IF_SVE_IC_3A_A: // ..........iiiiii ...gggnnnnnttttt -- SVE load and broadcast element
        case IF_SVE_IC_3A_B: // ..........iiiiii ...gggnnnnnttttt -- SVE load and broadcast element
        case IF_SVE_IC_3A_C: // ..........iiiiii ...gggnnnnnttttt -- SVE load and broadcast element
            imm  = emitGetInsSC(id);
            code = emitInsCodeSve(ins, fmt);
            code |= insEncodeReg_V<4, 0>(id->idReg1());   // ttttt
            code |= insEncodeReg_P<12, 10>(id->idReg2()); // ggg
            code |= insEncodeReg_R<9, 5>(id->idReg3());   // nnnnn
            code = insEncodeSveElemsize_dtypeh_dtypel(ins, fmt, optGetSveElemsize(id->idInsOpt()), code);

            switch (ins)
            {
                case INS_sve_ld1rw:
                    code |= insEncodeUimm_MultipleOf<21, 16, 4>(imm); // iiiiii
                    break;

                case INS_sve_ld1rh:
                case INS_sve_ld1rsh:
                    code |= insEncodeUimm_MultipleOf<21, 16, 2>(imm); // iiiiii
                    break;

                default:
                    code |= insEncodeUimm<21, 16>(imm); // iiiiii
                    break;
            }

            dst += emitOutput_Instr(dst, code);
            break;

        case IF_SVE_BI_2A: // ................ ......nnnnnddddd -- SVE constructive prefix (unpredicated)
        case IF_SVE_HH_2A: // ................ ......nnnnnddddd -- SVE2 FP8 upconverts
            code = emitInsCodeSve(ins, fmt);
            code |= insEncodeReg_V<4, 0>(id->idReg1()); // ddddd
            code |= insEncodeReg_V<9, 5>(id->idReg2()); // nnnnn
            dst += emitOutput_Instr(dst, code);
            break;

        case IF_SVE_CB_2A: // ........xx...... ......nnnnnddddd -- SVE broadcast general register
            code = emitInsCodeSve(ins, fmt);
            code |= insEncodeReg_V<4, 0>(id->idReg1());                      // ddddd
            code |= insEncodeReg_Rn(id->idReg2());                           // nnnnn
            code |= insEncodeSveElemsize(optGetSveElemsize(id->idInsOpt())); // xx
            dst += emitOutput_Instr(dst, code);
            break;

        case IF_SVE_BJ_2A: // ........xx...... ......nnnnnddddd -- SVE floating-point exponential accelerator
        case IF_SVE_CG_2A: // ........xx...... ......nnnnnddddd -- SVE reverse vector elements
        case IF_SVE_HF_2A: // ........xx...... ......nnnnnddddd -- SVE floating-point reciprocal estimate (unpredicated)
            code = emitInsCodeSve(ins, fmt);
            code |= insEncodeReg_V<4, 0>(id->idReg1());                      // ddddd
            code |= insEncodeReg_V<9, 5>(id->idReg2());                      // nnnnn
            code |= insEncodeSveElemsize(optGetSveElemsize(id->idInsOpt())); // xx
            dst += emitOutput_Instr(dst, code);
            break;

        case IF_SVE_CH_2A: // ........xx...... ......nnnnnddddd -- SVE unpack vector elements
            code = emitInsCodeSve(ins, fmt);
            code |= insEncodeReg_V<4, 0>(id->idReg1());                                     // ddddd
            code |= insEncodeReg_V<9, 5>(id->idReg2());                                     // nnnnn
            code |= insEncodeSveElemsize(optGetSveElemsize((insOpts)(id->idInsOpt() + 1))); // xx
            dst += emitOutput_Instr(dst, code);
            break;

        case IF_SVE_BF_2A: // ........xx.xxiii ......nnnnnddddd -- SVE bitwise shift by immediate (unpredicated)
        case IF_SVE_FT_2A: // ........xx.xxiii ......nnnnnddddd -- SVE2 bitwise shift and insert
        case IF_SVE_FU_2A: // ........xx.xxiii ......nnnnnddddd -- SVE2 bitwise shift right and accumulate
            imm  = emitGetInsSC(id);
            code = emitInsCodeSve(ins, fmt);
            code |= insEncodeReg_V<4, 0>(id->idReg1()); // ddddd
            code |= insEncodeReg_V<9, 5>(id->idReg2()); // nnnnn
            code |= insEncodeSveElemsizeWithShift_tszh_tszl_imm3(id->idInsOpt(), imm,
                                                                 emitInsIsVectorRightShift(ins)); // xx xxiii
            dst += emitOutput_Instr(dst, code);
            break;

        case IF_SVE_BX_2A: // ...........ixxxx ......nnnnnddddd -- sve_int_perm_dupq_i
            imm  = emitGetInsSC(id);
            code = emitInsCodeSve(ins, fmt);
            code |= insEncodeReg_V<4, 0>(id->idReg1());                            // ddddd
            code |= insEncodeReg_V<9, 5>(id->idReg2());                            // nnnnn
            code |= insEncodeSveElemsizeWithImmediate_i1_tsz(id->idInsOpt(), imm); // ixxxx
            dst += emitOutput_Instr(dst, code);
            break;

        case IF_SVE_BY_2A: // ............iiii ......mmmmmddddd -- sve_int_perm_extq
            imm  = emitGetInsSC(id);
            code = emitInsCodeSve(ins, fmt);
            code |= insEncodeReg_V<4, 0>(id->idReg1()); // ddddd
            code |= insEncodeReg_V<9, 5>(id->idReg2()); // mmmmm
            code |= insEncodeUimm<19, 16>(imm);         // iiii
            dst += emitOutput_Instr(dst, code);
            break;

        default:
            assert(!"Unexpected format");
            break;
    }

    return dst;
}

/*****************************************************************************
 *
 *  Prints the encoding for the Extend Type encoding
 */

void emitter::emitDispSveExtendOpts(insOpts opt)
{
    switch (opt)
    {
        case INS_OPTS_LSL:
            printf("lsl");
            break;

        case INS_OPTS_UXTW:
        case INS_OPTS_SCALABLE_S_UXTW:
        case INS_OPTS_SCALABLE_D_UXTW:
            printf("uxtw");
            break;

        case INS_OPTS_SXTW:
        case INS_OPTS_SCALABLE_S_SXTW:
        case INS_OPTS_SCALABLE_D_SXTW:
            printf("sxtw");
            break;

        default:
            assert(!"Bad value");
            break;
    }
}

/*****************************************************************************
 *
 *  Prints the encoding for the Extend Type encoding along with the N value
 */

void emitter::emitDispSveExtendOptsModN(insOpts opt, ssize_t imm)
{
    assert(imm >= 0 && imm <= 3);

    if (imm == 0 && opt != INS_OPTS_LSL)
    {
        emitDispSveExtendOpts(opt);
    }
    else if (imm > 0)
    {
        emitDispSveExtendOpts(opt);
        printf(" #%d", (int)imm);
    }
}

/*****************************************************************************
 *
 *  Prints the encoding for the <mod> or LSL encoding along with the N value
 *  This is for formats that have [<Xn|SP>, <Zm>.T, <mod>], [<Xn|SP>, <Zm>.T, <mod> #N], [<Xn|SP>, <Xm>, LSL #N],
 * [<Xn|SP>{, <Xm>, LSL #N}]
 */
void emitter::emitDispSveModAddr(instruction ins, regNumber reg1, regNumber reg2, insOpts opt, insFormat fmt)
{
    printf("[");

    if (isVectorRegister(reg1))
    {
        // If the overall instruction is working on 128-bit
        // registers, the size of this register for
        // the mod addr is always 64-bit.
        // Example: LD1Q    {<Zt>.Q }, <Pg>/Z, [<Zn>.D{, <Xm>}]
        if (opt == INS_OPTS_SCALABLE_Q)
        {
            emitDispSveReg(reg1, INS_OPTS_SCALABLE_D, reg2 != REG_ZR);
        }
        else
        {
            emitDispSveReg(reg1, opt, reg2 != REG_ZR);
        }
    }
    else
    {
        emitDispReg(reg1, EA_8BYTE, reg2 != REG_ZR);
    }

    if (isVectorRegister(reg2))
    {
        emitDispSveReg(reg2, opt, false);
    }
    else if (reg2 != REG_ZR)
    {
        emitDispReg(reg2, EA_8BYTE, false);
    }

    if (insOptsScalable32bitExtends(opt))
    {
        emitDispComma();
        emitDispSveExtendOptsModN(opt, insSveGetLslOrModN(ins, fmt));
    }
    // Omit 'lsl #N' only if the second register is ZR.
    else if ((reg2 != REG_ZR) && insSveIsLslN(ins, fmt))
    {
        emitDispComma();
        switch (insSveGetLslOrModN(ins, fmt))
        {
            case 4:
                printf("lsl #4");
                break;

            case 3:
                printf("lsl #3");
                break;

            case 2:
                printf("lsl #2");
                break;

            case 1:
                printf("lsl #1");
                break;

            default:
                assert(!"Invalid instruction");
                break;
        }
    }
    printf("]");
}

/*****************************************************************************
 *
 *  Prints the encoding for format [<Zn>.S{, #<imm>}]
 */
void emitter::emitDispSveImm(regNumber reg1, ssize_t imm, insOpts opt)
{
    printf("[");
    emitDispSveReg(reg1, opt, imm != 0);
    if (imm != 0)
    {
        // This does not have to be printed as hex.
        // We only do it because the capstone disassembly displays this immediate as hex.
        // We could not modify capstone without affecting other cases.
        emitDispImm(imm, false, /* alwaysHex */ true);
    }
    printf("]");
}

/*****************************************************************************
 *
 *  Prints the encoding for format [<Xn|SP>{, #<imm>, MUL VL}]
 */
void emitter::emitDispSveImmMulVl(regNumber reg1, ssize_t imm)
{
    printf("[");
    emitDispReg(reg1, EA_8BYTE, imm != 0);
    if (imm != 0)
    {
        emitDispImm(imm, true);
        printf("mul vl");
    }
    printf("]");
}

/*****************************************************************************
 *
 *  Prints the encoding for format [<Zn>.D{, #<imm>}]
 */
void emitter::emitDispSveImmIndex(regNumber reg1, insOpts opt, ssize_t imm)
{
    printf("[");
    if (isVectorRegister(reg1))
    {
        emitDispSveReg(reg1, opt, imm != 0);
    }
    else
    {
        emitDispReg(reg1, EA_8BYTE, imm != 0);
    }
    if (imm != 0)
    {
        // This does not have to be printed as hex.
        // We only do it because the capstone disassembly displays this immediate as hex.
        // We could not modify capstone without affecting other cases.
        emitDispImm(imm, false, /* alwaysHex */ (imm > 31));
    }
    printf("]");
}

//------------------------------------------------------------------------
// emitDispSveReg: Display a scalable vector register name
//
void emitter::emitDispSveReg(regNumber reg, bool addComma)
{
    assert(isVectorRegister(reg));
    printf(emitSveRegName(reg));

    if (addComma)
        emitDispComma();
}

//------------------------------------------------------------------------
// emitDispSveReg: Display a scalable vector register name with an arrangement suffix
//
void emitter::emitDispSveReg(regNumber reg, insOpts opt, bool addComma)
{
    assert(isVectorRegister(reg));
    printf(emitSveRegName(reg));

    if (opt != INS_OPTS_NONE)
    {
        assert(insOptsScalable(opt) || insOptsScalable32bitExtends(opt));
        emitDispArrangement(opt);
    }

    if (addComma)
        emitDispComma();
}

//------------------------------------------------------------------------
// emitDispSveRegIndex: Display a scalable vector register with indexed element
//
void emitter::emitDispSveRegIndex(regNumber reg, ssize_t index, bool addComma)
{
    assert(isVectorRegister(reg));
    printf(emitSveRegName(reg));
    emitDispElementIndex(index, addComma);
}

//------------------------------------------------------------------------
// emitDispSveConsecutiveRegList: Display a SVE consecutive vector register list
//
void emitter::emitDispSveConsecutiveRegList(regNumber firstReg, unsigned listSize, insOpts opt, bool addComma)
{
    assert(isVectorRegister(firstReg));

    regNumber currReg = firstReg;

    assert(listSize > 0);

    printf("{ ");
    // We do not want the short-hand for list size of 1 or 2.
    if ((listSize <= 2) || (((unsigned)currReg + listSize - 1) > (unsigned)REG_V31))
    {
        for (unsigned i = 0; i < listSize; i++)
        {
            const bool notLastRegister = (i != listSize - 1);
            emitDispSveReg(currReg, opt, notLastRegister);
            currReg = (currReg == REG_V31) ? REG_V0 : REG_NEXT(currReg);
        }
    }
    else
    {
        // short-hand. example: { z0.s - z2.s } which is the same as { z0.s, z1.s, z2.s }
        emitDispSveReg(currReg, opt, false);
        printf(" - ");
        emitDispSveReg((regNumber)(currReg + listSize - 1), opt, false);
    }
    printf(" }");

    if (addComma)
    {
        emitDispComma();
    }
}

//------------------------------------------------------------------------
// emitSveRegName: Returns a scalable vector register name.
//
// Arguments:
//    reg - A SIMD and floating-point register.
//
// Return value:
//    A string that represents a scalable vector register name.
//
const char* emitter::emitSveRegName(regNumber reg) const
{
    assert((reg >= REG_V0) && (reg <= REG_V31));

    int index = (int)reg - (int)REG_V0;

    return zRegNames[index];
}

//------------------------------------------------------------------------
// emitPredicateRegName: Returns a predicate register name.
//
// Arguments:
//    reg - A predicate register.
//
// Return value:
//    A string that represents a predicate register name.
//
const char* emitter::emitPredicateRegName(regNumber reg, PredicateType ptype)
{
    assert((reg >= REG_P0) && (reg <= REG_P15));

    const int  index     = (int)reg - (int)REG_P0;
    const bool usePnRegs = (ptype == PREDICATE_N) || (ptype == PREDICATE_N_SIZED);

    return usePnRegs ? pnRegNames[index] : pRegNames[index];
}

//------------------------------------------------------------------------
// emitDispPredicateReg: Display a predicate register name with with an arrangement suffix
//
void emitter::emitDispPredicateReg(regNumber reg, PredicateType ptype, insOpts opt, bool addComma)
{
    assert(isPredicateRegister(reg));
    printf(emitPredicateRegName(reg, ptype));

    if (ptype == PREDICATE_MERGE)
    {
        printf("/m");
    }
    else if (ptype == PREDICATE_ZERO)
    {
        printf("/z");
    }
    else if (ptype == PREDICATE_SIZED || ptype == PREDICATE_N_SIZED)
    {
        emitDispElemsize(optGetSveElemsize(opt));
    }

    if (addComma)
        emitDispComma();
}

//------------------------------------------------------------------------
// emitDispPredicateRegPair: Display a pair of predicate registers
//
void emitter::emitDispPredicateRegPair(regNumber reg, insOpts opt)
{
    printf("{ ");
    emitDispPredicateReg(reg, PREDICATE_SIZED, opt, true);
    emitDispPredicateReg((regNumber)((unsigned)reg + 1), PREDICATE_SIZED, opt, false);
    printf(" }, ");
}

//------------------------------------------------------------------------
// emitDispLowPredicateReg: Display a low predicate register name with with an arrangement suffix
//
void emitter::emitDispLowPredicateReg(regNumber reg, PredicateType ptype, insOpts opt, bool addComma)
{
    assert(isLowPredicateRegister(reg));
    reg = (regNumber)((((unsigned)reg - REG_PREDICATE_FIRST) & 0x7) + REG_PREDICATE_FIRST);
    emitDispPredicateReg(reg, ptype, opt, addComma);
}

//------------------------------------------------------------------------
// emitDispLowPredicateRegPair: Display a pair of low predicate registers
//
void emitter::emitDispLowPredicateRegPair(regNumber reg, insOpts opt)
{
    assert(isLowPredicateRegister(reg));

    printf("{ ");
    const unsigned baseRegNum = ((unsigned)reg - REG_PREDICATE_FIRST) & 0x7;
    const unsigned regNum     = (baseRegNum * 2) + REG_PREDICATE_FIRST;
    emitDispPredicateReg((regNumber)regNum, PREDICATE_SIZED, opt, true);
    emitDispPredicateReg((regNumber)(regNum + 1), PREDICATE_SIZED, opt, false);
    printf(" }, ");
}

//------------------------------------------------------------------------
// emitDispVectorLengthSpecifier: Display the vector length specifier
//
void emitter::emitDispVectorLengthSpecifier(instrDesc* id)
{
    assert(id != nullptr);
    assert(insOptsScalableStandard(id->idInsOpt()));

    if (id->idVectorLength4x())
    {
        printf("vlx4");
    }
    else
    {
        printf("vlx2");
    }
}

/*****************************************************************************
 *
 *  Display an insSvePattern
 */
void emitter::emitDispSvePattern(insSvePattern pattern, bool addComma)
{
    printf("%s", svePatternNames[pattern]);

    if (addComma)
    {
        emitDispComma();
    }
}

/*****************************************************************************
 *
 *  Display an insSvePrfop
 */
void emitter::emitDispSvePrfop(insSvePrfop prfop, bool addComma)
{
    switch (prfop)
    {
        case SVE_PRFOP_PLDL1KEEP:
            printf("pldl1keep");
            break;

        case SVE_PRFOP_PLDL1STRM:
            printf("pldl1strm");
            break;

        case SVE_PRFOP_PLDL2KEEP:
            printf("pldl2keep");
            break;

        case SVE_PRFOP_PLDL2STRM:
            printf("pldl2strm");
            break;

        case SVE_PRFOP_PLDL3KEEP:
            printf("pldl3keep");
            break;

        case SVE_PRFOP_PLDL3STRM:
            printf("pldl3strm");
            break;

        case SVE_PRFOP_PSTL1KEEP:
            printf("pstl1keep");
            break;

        case SVE_PRFOP_PSTL1STRM:
            printf("pstl1strm");
            break;

        case SVE_PRFOP_PSTL2KEEP:
            printf("pstl2keep");
            break;

        case SVE_PRFOP_PSTL2STRM:
            printf("pstl2strm");
            break;

        case SVE_PRFOP_PSTL3KEEP:
            printf("pstl3keep");
            break;

        case SVE_PRFOP_PSTL3STRM:
            printf("pstl3strm");
            break;

        case SVE_PRFOP_CONST6:
            printf("#6");
            break;

        case SVE_PRFOP_CONST7:
            printf("#7");
            break;

        case SVE_PRFOP_CONST14:
            printf("#0xE");
            break;

        case SVE_PRFOP_CONST15:
            printf("#0xF");
            break;

        default:
            assert(!"Invalid prfop");
            break;
    }

    if (addComma)
    {
        emitDispComma();
    }
}

/*****************************************************************************
 *
 *  Returns the encoding to set the vector length specifier (vl) for an Arm64 SVE instruction
 */

/*static*/ emitter::code_t emitter::insEncodeVectorLengthSpecifier(instrDesc* id)
{
    assert(id != nullptr);
    assert(insOptsScalableStandard(id->idInsOpt()));

    if (id->idVectorLength4x())
    {
        switch (id->idInsFmt())
        {
            case IF_SVE_DL_2A:
                return 0x400; // set the bit at location 10
            case IF_SVE_DY_3A:
                return 0x2000; // set the bit at location 13
            default:
                assert(!"Unexpected format");
                break;
        }
    }

    return 0;
}

/*****************************************************************************
 *
 *  Return an encoding for the specified predicate type used in '16' position.
 */

/*static*/ emitter::code_t emitter::insEncodePredQualifier_16(bool merge)
{
    return merge ? 1 << 16 : 0;
}

/*****************************************************************************
 *
 *  Return an encoding for the specified predicate type used in '4' position.
 */

/*static*/ emitter::code_t emitter::insEncodePredQualifier_4(bool merge)
{
    return merge ? 1 << 4 : 0;
}

//  For the given 'elemsize' returns the 'arrangement' when used in a SVE vector register arrangement.
//  Asserts and returns INS_OPTS_NONE if an invalid 'elemsize' is passed
//
/*static*/ insOpts emitter::optGetSveInsOpt(emitAttr elemsize)
{
    switch (elemsize)
    {
        case EA_1BYTE:
            return INS_OPTS_SCALABLE_B;

        case EA_2BYTE:
            return INS_OPTS_SCALABLE_H;

        case EA_4BYTE:
            return INS_OPTS_SCALABLE_S;

        case EA_8BYTE:
            return INS_OPTS_SCALABLE_D;

        case EA_16BYTE:
            return INS_OPTS_SCALABLE_Q;

        default:
            assert(!"Invalid emitAttr for sve vector register");
            return INS_OPTS_NONE;
    }
}

//  For the given 'arrangement' returns the 'elemsize' specified by the SVE vector register arrangement
//  asserts and returns EA_UNKNOWN if an invalid 'arrangement' value is passed
//
/*static*/ emitAttr emitter::optGetSveElemsize(insOpts arrangement)
{
    switch (arrangement)
    {
        case INS_OPTS_SCALABLE_B:
            return EA_1BYTE;

        case INS_OPTS_SCALABLE_H:
            return EA_2BYTE;

        case INS_OPTS_SCALABLE_S:
        case INS_OPTS_SCALABLE_S_UXTW:
        case INS_OPTS_SCALABLE_S_SXTW:
            return EA_4BYTE;

        case INS_OPTS_SCALABLE_D:
        case INS_OPTS_SCALABLE_D_UXTW:
        case INS_OPTS_SCALABLE_D_SXTW:
            return EA_8BYTE;

        case INS_OPTS_SCALABLE_Q:
            return EA_16BYTE;

        default:
            assert(!"Invalid insOpt for vector register");
            return EA_UNKNOWN;
    }
}

/*static*/ insOpts emitter::optWidenSveElemsizeArrangement(insOpts arrangement)
{
    switch (arrangement)
    {
        case INS_OPTS_SCALABLE_B:
            return INS_OPTS_SCALABLE_H;

        case INS_OPTS_SCALABLE_H:
            return INS_OPTS_SCALABLE_S;

        case INS_OPTS_SCALABLE_S:
            return INS_OPTS_SCALABLE_D;

        default:
            assert(!" invalid 'arrangement' value");
            return INS_OPTS_NONE;
    }
}

/*static*/ insOpts emitter::optSveToQuadwordElemsizeArrangement(insOpts arrangement)
{
    switch (arrangement)
    {
        case INS_OPTS_SCALABLE_B:
            return INS_OPTS_16B;

        case INS_OPTS_SCALABLE_H:
            return INS_OPTS_8H;

        case INS_OPTS_SCALABLE_S:
            return INS_OPTS_4S;

        case INS_OPTS_SCALABLE_D:
            return INS_OPTS_2D;

        default:
            assert(!" invalid 'arrangement' value");
            return INS_OPTS_NONE;
    }
}

/*****************************************************************************
 *
 *  Expands an option that has different size operands (INS_OPTS_*_TO_*) into
 *  a pair of scalable options where the first describes the size of the
 *  destination operand and the second describes the size of the source operand.
 */

/*static*/ void emitter::optExpandConversionPair(insOpts opt, insOpts& dst, insOpts& src)
{
    dst = INS_OPTS_NONE;
    src = INS_OPTS_NONE;

    switch (opt)
    {
        case INS_OPTS_H_TO_S:
            dst = INS_OPTS_SCALABLE_S;
            src = INS_OPTS_SCALABLE_H;
            break;
        case INS_OPTS_S_TO_H:
            dst = INS_OPTS_SCALABLE_H;
            src = INS_OPTS_SCALABLE_S;
            break;
        case INS_OPTS_S_TO_D:
            dst = INS_OPTS_SCALABLE_D;
            src = INS_OPTS_SCALABLE_S;
            break;
        case INS_OPTS_D_TO_S:
            dst = INS_OPTS_SCALABLE_S;
            src = INS_OPTS_SCALABLE_D;
            break;
        case INS_OPTS_H_TO_D:
            dst = INS_OPTS_SCALABLE_D;
            src = INS_OPTS_SCALABLE_H;
            break;
        case INS_OPTS_D_TO_H:
            dst = INS_OPTS_SCALABLE_H;
            src = INS_OPTS_SCALABLE_D;
            break;
        case INS_OPTS_SCALABLE_H:
            dst = INS_OPTS_SCALABLE_H;
            src = INS_OPTS_SCALABLE_H;
            break;
        case INS_OPTS_SCALABLE_S:
            dst = INS_OPTS_SCALABLE_S;
            src = INS_OPTS_SCALABLE_S;
            break;
        case INS_OPTS_SCALABLE_D:
            dst = INS_OPTS_SCALABLE_D;
            src = INS_OPTS_SCALABLE_D;
            break;
        default:
            noway_assert(!"unreachable");
            break;
    }

    assert(dst != INS_OPTS_NONE && src != INS_OPTS_NONE);
    return;
}

#ifdef DEBUG
/*****************************************************************************
 *
 *  The following is called for each recorded SVE instruction -- use for debugging.
 */
void emitter::emitInsSveSanityCheck(instrDesc* id)
{
    switch (id->idInsFmt())
    {
        ssize_t imm;

        case IF_SVE_CK_2A: // ................ .......NNNN.DDDD -- SVE unpack predicate elements
            assert(isPredicateRegister(id->idReg1())); // DDDD
            assert(isPredicateRegister(id->idReg2())); // NNNN
            break;

        // Scalable.
        case IF_SVE_AA_3A: // ........xx...... ...gggmmmmmddddd
        case IF_SVE_CM_3A: // ........xx...... ...gggmmmmmddddd -- SVE conditionally broadcast element to vector
            assert(insOptsScalableStandard(id->idInsOpt())); // xx
            assert(isVectorRegister(id->idReg1()));          // ddddd
            assert(isLowPredicateRegister(id->idReg2()));    // ggg
            assert(isVectorRegister(id->idReg3()));          // mmmmm
            assert(isScalableVectorSize(id->idOpSize()));
            break;

        // Scalable, .S or .D.
        case IF_SVE_AC_3A: // ........xx...... ...gggmmmmmddddd -- SVE integer divide vectors (predicated)
        case IF_SVE_CL_3A: // ........xx...... ...gggnnnnnddddd -- SVE compress active elements
            assert(insOptsScalableWords(id->idInsOpt())); // xx
            assert(isVectorRegister(id->idReg1()));       // ddddd
            assert(isLowPredicateRegister(id->idReg2())); // ggg
            assert(isVectorRegister(id->idReg3()));       // mmmmm
            assert(isScalableVectorSize(id->idOpSize()));
            break;

        // Scalable, Merge or Zero predicate.
        case IF_SVE_AH_3A: // ........xx.....M ...gggnnnnnddddd -- SVE constructive prefix (predicated)
            assert(insOptsScalableStandard(id->idInsOpt()));
            assert(isVectorRegister(id->idReg1()));       // nnnnn
            assert(isLowPredicateRegister(id->idReg2())); // ggg
            assert(isVectorRegister(id->idReg3()));       // ddddd
            assert(isScalableVectorSize(id->idOpSize()));
            break;

        // Scalable, with shift immediate.
        case IF_SVE_AM_2A: // ........xx...... ...gggxxiiiddddd -- SVE bitwise shift by immediate (predicated)
            assert(insOptsScalableStandard(id->idInsOpt()));
            assert(isVectorRegister(id->idReg1()));       // ddddd
            assert(isLowPredicateRegister(id->idReg2())); // ggg
            assert(isValidVectorShiftAmount(emitGetInsSC(id), optGetSveElemsize(id->idInsOpt()), true));
            assert(isScalableVectorSize(id->idOpSize()));
            break;

        // Scalable Wide.
        case IF_SVE_AO_3A: // ........xx...... ...gggmmmmmddddd -- SVE bitwise shift by wide elements (predicated)
            assert(insOptsScalableWide(id->idInsOpt()));  // xx
            assert(isVectorRegister(id->idReg1()));       // ddddd
            assert(isLowPredicateRegister(id->idReg2())); // ggg
            assert(isVectorRegister(id->idReg3()));       // mmmmm
            assert(isScalableVectorSize(id->idOpSize()));
            break;

        // Scalable to/from SIMD scalar.
        case IF_SVE_AF_3A: // ........xx...... ...gggnnnnnddddd -- SVE bitwise logical reduction (predicated)
        case IF_SVE_AK_3A: // ........xx...... ...gggnnnnnddddd -- SVE integer min/max reduction (predicated)
        case IF_SVE_CN_3A: // ........xx...... ...gggmmmmmddddd -- SVE conditionally extract element to SIMD&FP scalar
        case IF_SVE_CP_3A: // ........xx...... ...gggnnnnnddddd -- SVE copy SIMD&FP scalar register to vector
                           // (predicated)
        case IF_SVE_CR_3A: // ........xx...... ...gggnnnnnddddd -- SVE extract element to SIMD&FP scalar register
            assert(insOptsScalableStandard(id->idInsOpt())); // xx
            assert(isVectorRegister(id->idReg1()));          // ddddd
            assert(isLowPredicateRegister(id->idReg2()));    // ggg
            assert(isVectorRegister(id->idReg3()));          // mmmmm
            assert(isScalableVectorSize(id->idOpSize()));
            break;

        // Scalable to FP SIMD scalar.
        case IF_SVE_HE_3A: // ........xx...... ...gggnnnnnddddd -- SVE floating-point recursive reduction
        case IF_SVE_HJ_3A: // ........xx...... ...gggmmmmmddddd -- SVE floating-point serial reduction (predicated)
            assert(insOptsScalableFloat(id->idInsOpt())); // xx
            assert(isVectorRegister(id->idReg1()));       // ddddd
            assert(isLowPredicateRegister(id->idReg2())); // ggg
            assert(isVectorRegister(id->idReg3()));       // mmmmm
            assert(isScalableVectorSize(id->idOpSize()));
            break;

        // Scalable to general register.
        case IF_SVE_CO_3A: // ........xx...... ...gggmmmmmddddd -- SVE conditionally extract element to general register
        case IF_SVE_CS_3A: // ........xx...... ...gggnnnnnddddd -- SVE extract element to general register
            assert(insOptsScalableStandard(id->idInsOpt())); // xx
            assert(isGeneralRegister(id->idReg1()));         // ddddd
            assert(isLowPredicateRegister(id->idReg2()));    // ggg
            assert(isVectorRegister(id->idReg3()));          // mmmmm
            assert(isValidScalarDatasize(id->idOpSize()));
            break;

        // Scalable, 4 regs (location of reg3 and reg4 can switch)
        case IF_SVE_AR_4A: // ........xx.mmmmm ...gggnnnnnddddd -- SVE integer multiply-accumulate writing addend
                           // (predicated)
        case IF_SVE_AS_4A: // ........xx.mmmmm ...gggaaaaaddddd -- SVE integer multiply-add writing multiplicand
                           // (predicated)
        case IF_SVE_GI_4A: // ........xx.mmmmm ...gggnnnnnddddd -- SVE2 histogram generation (vector)
        case IF_SVE_HU_4A: // ........xx.mmmmm ...gggnnnnnddddd -- SVE floating-point multiply-accumulate writing addend
            assert(insOptsScalableStandard(id->idInsOpt())); // xx
            assert(isVectorRegister(id->idReg1()));          // ddddd
            assert(isLowPredicateRegister(id->idReg2()));    // ggg
            assert(isVectorRegister(id->idReg3()));
            assert(isVectorRegister(id->idReg4()));
            assert(isScalableVectorSize(id->idOpSize()));
            break;

        // Scalable, unpredicated
        case IF_SVE_AT_3A:   // ........xx.mmmmm ......nnnnnddddd
        case IF_SVE_BG_3A:   // ........xx.mmmmm ......nnnnnddddd -- SVE bitwise shift by wide elements (unpredicated)
        case IF_SVE_BZ_3A:   // ........xx.mmmmm ......nnnnnddddd -- SVE table lookup (three sources)
        case IF_SVE_BZ_3A_A: // ........xx.mmmmm ......nnnnnddddd -- SVE table lookup (three sources)
        case IF_SVE_EH_3A:   // ........xx.mmmmm ......nnnnnddddd -- SVE integer dot product (unpredicated)
        case IF_SVE_EL_3A:   // ........xx.mmmmm ......nnnnnddddd
        case IF_SVE_EM_3A:   // ........xx.mmmmm ......nnnnnddddd -- SVE2 saturating multiply-add high
        case IF_SVE_EX_3A:   // ........xx.mmmmm ......nnnnnddddd -- SVE permute vector elements (quadwords)
        case IF_SVE_FL_3A:   // ........xx.mmmmm ......nnnnnddddd
        case IF_SVE_FM_3A:   // ........xx.mmmmm ......nnnnnddddd -- SVE2 integer add/subtract wide
        case IF_SVE_FW_3A:   // ........xx.mmmmm ......nnnnnddddd -- SVE2 integer absolute difference and accumulate
        case IF_SVE_GC_3A:   // ........xx.mmmmm ......nnnnnddddd -- SVE2 integer add/subtract narrow high part
        case IF_SVE_GF_3A:   // ........xx.mmmmm ......nnnnnddddd -- SVE2 histogram generation (segment)
            assert(insOptsScalableStandard(id->idInsOpt())); // xx
            assert(isVectorRegister(id->idReg1()));          // ddddd
            assert(isVectorRegister(id->idReg2()));          // nnnnn
            assert(isVectorRegister(id->idReg3()));          // mmmmm
            assert(isScalableVectorSize(id->idOpSize()));
            break;

        // Scalable, no predicates. General purpose source registers
        case IF_SVE_BA_3A: // ........xx.mmmmm ......nnnnnddddd -- SVE index generation (register start, register
                           // increment)
            assert(insOptsScalableStandard(id->idInsOpt())); // xx
            assert(isVectorRegister(id->idReg1()));          // ddddd
            assert(isGeneralRegisterOrZR(id->idReg2()));     // nnnnn
            assert(isGeneralRegisterOrZR(id->idReg3()));     // mmmmm
            assert(isValidScalarDatasize(id->idOpSize()));
            break;

        case IF_SVE_BH_3A: // .........x.mmmmm ....hhnnnnnddddd -- SVE address generation
            assert(id->idInsOpt() == INS_OPTS_SCALABLE_S || id->idInsOpt() == INS_OPTS_SCALABLE_D);
            assert(isVectorRegister(id->idReg1()));   // ddddd
            assert(isVectorRegister(id->idReg2()));   // nnnnn
            assert(isVectorRegister(id->idReg3()));   // mmmmm
            assert(isValidUimm<2>(emitGetInsSC(id))); // hh
            break;

        case IF_SVE_BH_3B:   // ...........mmmmm ....hhnnnnnddddd -- SVE address generation
        case IF_SVE_BH_3B_A: // ...........mmmmm ....hhnnnnnddddd -- SVE address generation
            assert(id->idInsOpt() == INS_OPTS_SCALABLE_D_SXTW || id->idInsOpt() == INS_OPTS_SCALABLE_D_UXTW);
            assert(isVectorRegister(id->idReg1()));   // ddddd
            assert(isVectorRegister(id->idReg2()));   // nnnnn
            assert(isVectorRegister(id->idReg3()));   // mmmmm
            assert(isValidUimm<2>(emitGetInsSC(id))); // hh
            break;

        case IF_SVE_BL_1A: // ............iiii ......pppppddddd -- SVE element count
        case IF_SVE_BM_1A: // ............iiii ......pppppddddd -- SVE inc/dec register by element count
            assert(id->idInsOpt() == INS_OPTS_NONE);
            assert(isGeneralRegister(id->idReg1()));
            assert(id->idOpSize() == EA_8BYTE);
            assert(isValidUimmFrom1<4>(emitGetInsSC(id)));
            break;

        case IF_SVE_BN_1A: // ............iiii ......pppppddddd -- SVE inc/dec vector by element count
        case IF_SVE_BP_1A: // ............iiii ......pppppddddd -- SVE saturating inc/dec vector by element count
            assert(insOptsScalableAtLeastHalf(id->idInsOpt()));
            assert(isVectorRegister(id->idReg1()));
            assert(isScalableVectorSize(id->idOpSize()));
            assert(isValidUimmFrom1<4>(emitGetInsSC(id)));
            break;

        case IF_SVE_BS_1A: // ..............ii iiiiiiiiiiiddddd -- SVE bitwise logical with immediate (unpredicated)
        case IF_SVE_BT_1A: // ..............ii iiiiiiiiiiiddddd -- SVE broadcast bitmask immediate
            imm = emitGetInsSC(id);
            assert(insOptsScalableStandard(id->idInsOpt()));
            assert(isVectorRegister(id->idReg1())); // ddddd
            assert(isValidImmNRS(imm, optGetSveElemsize(id->idInsOpt())));
            break;

        case IF_SVE_BO_1A: // ...........Xiiii ......pppppddddd -- SVE saturating inc/dec register by element count
            assert(id->idInsOpt() == INS_OPTS_NONE);
            assert(isGeneralRegister(id->idReg1()));
            assert(isValidGeneralDatasize(id->idOpSize()));
            assert(isValidUimmFrom1<4>(emitGetInsSC(id)));
            break;

        case IF_SVE_BQ_2A: // ...........iiiii ...iiinnnnnddddd -- SVE extract vector (immediate offset, destructive)
        case IF_SVE_BQ_2B: // ...........iiiii ...iiimmmmmddddd -- SVE extract vector (immediate offset, destructive)
            assert(id->idInsOpt() == INS_OPTS_SCALABLE_B);
            assert(isVectorRegister(id->idReg1()));   // ddddd
            assert(isVectorRegister(id->idReg2()));   // nnnnn
            assert(isValidUimm<8>(emitGetInsSC(id))); // iiiii iii
            break;

        case IF_SVE_BU_2A: // ........xx..gggg ...iiiiiiiiddddd -- SVE copy floating-point immediate (predicated)
        {
            imm = emitGetInsSC(id);
            floatImm8 fpImm;
            fpImm.immFPIVal = (unsigned)imm;
            assert(insOptsScalableAtLeastHalf(id->idInsOpt()));
            assert(isVectorRegister(id->idReg1()));                           // ddddd
            assert(isValidSimm<8>((ssize_t)emitDecodeFloatImm8(fpImm)));      // iiiiiiii
            assert(isPredicateRegister(id->idReg2()));                        // gggg
            assert(isValidVectorElemsize(optGetSveElemsize(id->idInsOpt()))); // xx
            break;
        }

        case IF_SVE_BV_2A:   // ........xx..gggg ..hiiiiiiiiddddd -- SVE copy integer immediate (predicated)
        case IF_SVE_BV_2A_J: // ........xx..gggg ..hiiiiiiiiddddd -- SVE copy integer immediate (predicated)
            imm = emitGetInsSC(id);
            assert(insOptsScalableStandard(id->idInsOpt()));                  // xx
            assert(isVectorRegister(id->idReg1()));                           // ddddd
            assert(isPredicateRegister(id->idReg2()));                        // gggg
            assert(isValidVectorElemsize(optGetSveElemsize(id->idInsOpt()))); // xx
            assert(isValidSimm<8>(imm));                                      // iiiiiiii
            break;

        case IF_SVE_BV_2B: // ........xx..gggg ...........ddddd -- SVE copy integer immediate (predicated)
            assert(insOptsScalableAtLeastHalf(id->idInsOpt())); // xx
            assert(isVectorRegister(id->idReg1()));             // ddddd
            assert(isPredicateRegister(id->idReg2()));          // gggg
            break;

        case IF_SVE_CE_2A: // ................ ......nnnnn.DDDD -- SVE move predicate from vector
            assert(isPredicateRegister(id->idReg1())); // DDDD
            assert(isVectorRegister(id->idReg2()));    // nnnnn
            break;

        case IF_SVE_CE_2B: // .........i...ii. ......nnnnn.DDDD -- SVE move predicate from vector
            assert(isPredicateRegister(id->idReg1())); // DDDD
            assert(isVectorRegister(id->idReg2()));    // nnnnn
            assert(isValidUimm<3>(emitGetInsSC(id)));
            break;

        case IF_SVE_CE_2C: // ..............i. ......nnnnn.DDDD -- SVE move predicate from vector
            assert(isPredicateRegister(id->idReg1())); // DDDD
            assert(isVectorRegister(id->idReg2()));    // nnnnn
            assert(isValidUimm<1>(emitGetInsSC(id)));  // i
            break;

        case IF_SVE_CE_2D: // .............ii. ......nnnnn.DDDD -- SVE move predicate from vector
            assert(isPredicateRegister(id->idReg1())); // DDDD
            assert(isVectorRegister(id->idReg2()));    // nnnnn
            assert(isValidUimm<3>(emitGetInsSC(id)));  // ii
            break;

        case IF_SVE_CF_2A: // ................ .......NNNNddddd -- SVE move predicate into vector
            assert(isVectorRegister(id->idReg1()));    // ddddd
            assert(isPredicateRegister(id->idReg2())); // NNNN
            break;

        case IF_SVE_CF_2B: // .........i...ii. .......NNNNddddd -- SVE move predicate into vector
            assert(isVectorRegister(id->idReg1()));    // ddddd
            assert(isPredicateRegister(id->idReg2())); // NNNN
            assert(isValidUimm<3>(emitGetInsSC(id)));
            break;

        case IF_SVE_CF_2C: // ..............i. .......NNNNddddd -- SVE move predicate into vector
            assert(isVectorRegister(id->idReg1()));    // ddddd
            assert(isPredicateRegister(id->idReg2())); // NNNN
            assert(isValidUimm<1>(emitGetInsSC(id)));  // i
            break;

        case IF_SVE_CF_2D: // .............ii. .......NNNNddddd -- SVE move predicate into vector
            assert(isVectorRegister(id->idReg1()));    // ddddd
            assert(isPredicateRegister(id->idReg2())); // NNNN
            assert(isValidUimm<2>(emitGetInsSC(id)));  // ii
            break;

        case IF_SVE_CC_2A: // ........xx...... ......mmmmmddddd -- SVE insert SIMD&FP scalar register
            assert(insOptsScalableStandard(id->idInsOpt()));
            assert(isVectorRegister(id->idReg1())); // ddddd
            assert(isVectorRegister(id->idReg2())); // mmmmm
            break;

        case IF_SVE_CD_2A: // ........xx...... ......mmmmmddddd -- SVE insert general register
            assert(insOptsScalableStandard(id->idInsOpt()));
            assert(isVectorRegister(id->idReg1()));      // ddddd
            assert(isGeneralRegisterOrZR(id->idReg2())); // mmmmm
            break;

        case IF_SVE_CI_3A: // ........xx..MMMM .......NNNN.DDDD -- SVE permute predicate elements
            assert(insOptsScalableStandard(id->idInsOpt()));
            assert(isPredicateRegister(id->idReg1())); // DDDD
            assert(isPredicateRegister(id->idReg2())); // NNNN
            assert(isPredicateRegister(id->idReg3())); // MMMM
            break;

        case IF_SVE_CJ_2A: // ........xx...... .......NNNN.DDDD -- SVE reverse predicate elements
            assert(isScalableVectorSize(id->idOpSize()));
            assert(insOptsScalableStandard(id->idInsOpt())); // xx
            assert(isPredicateRegister(id->idReg1()));       // DDDD
            assert(isPredicateRegister(id->idReg2()));       // NNNN
            break;

        case IF_SVE_CT_3A:                          // ................ ...gggnnnnnddddd -- SVE reverse doublewords
            assert(isVectorRegister(id->idReg1())); // ddddd
            assert(isLowPredicateRegister(id->idReg2())); // ggg
            assert(isVectorRegister(id->idReg3()));       // nnnnn
            break;

        // Scalable, 4 regs, to predicate register.
        case IF_SVE_CX_4A: // ........xx.mmmmm ...gggnnnnn.DDDD -- SVE integer compare vectors
            assert(isScalableVectorSize(id->idOpSize()));
            assert(insOptsScalableStandard(id->idInsOpt())); // xx
            assert(isPredicateRegister(id->idReg1()));       // DDDD
            assert(isLowPredicateRegister(id->idReg2()));    // ggg
            assert(isVectorRegister(id->idReg3()));          // nnnnn
            assert(isVectorRegister(id->idReg4()));          // mmmmm
            break;

        case IF_SVE_CX_4A_A: // ........xx.mmmmm ...gggnnnnn.DDDD -- SVE integer compare vectors
            assert(isScalableVectorSize(id->idOpSize()));
            assert(insOptsScalableWide(id->idInsOpt()));  // xx
            assert(isPredicateRegister(id->idReg1()));    // DDDD
            assert(isLowPredicateRegister(id->idReg2())); // ggg
            assert(isVectorRegister(id->idReg3()));       // nnnnn
            assert(isVectorRegister(id->idReg4()));       // mmmmm
            break;

        case IF_SVE_CY_3A: // ........xx.iiiii ...gggnnnnn.DDDD -- SVE integer compare with signed immediate
            assert(isScalableVectorSize(id->idOpSize()));
            assert(insOptsScalableStandard(id->idInsOpt()));
            assert(isPredicateRegister(id->idReg1()));    // DDDD
            assert(isLowPredicateRegister(id->idReg2())); // ggg
            assert(isVectorRegister(id->idReg3()));       // nnnnn
            assert(isValidSimm<5>(emitGetInsSC(id)));     // iiiii
            break;

        case IF_SVE_CY_3B: // ........xx.iiiii ii.gggnnnnn.DDDD -- SVE integer compare with unsigned immediate
            assert(isScalableVectorSize(id->idOpSize()));
            assert(insOptsScalableStandard(id->idInsOpt()));
            assert(isPredicateRegister(id->idReg1()));    // DDDD
            assert(isLowPredicateRegister(id->idReg2())); // ggg
            assert(isVectorRegister(id->idReg3()));       // nnnnn
            assert(isValidUimm<7>(emitGetInsSC(id)));     // iiiii
            break;

        case IF_SVE_BR_3B:   // ...........mmmmm ......nnnnnddddd -- SVE permute vector segments
        case IF_SVE_FN_3B:   // ...........mmmmm ......nnnnnddddd -- SVE2 integer multiply long
        case IF_SVE_FO_3A:   // ...........mmmmm ......nnnnnddddd -- SVE integer matrix multiply accumulate
        case IF_SVE_AT_3B:   // ...........mmmmm ......nnnnnddddd -- SVE integer add/subtract vectors (unpredicated)
        case IF_SVE_BD_3B:   // ...........mmmmm ......nnnnnddddd -- SVE2 integer multiply vectors (unpredicated)
        case IF_SVE_EF_3A:   // ...........mmmmm ......nnnnnddddd -- SVE two-way dot product
        case IF_SVE_EI_3A:   // ...........mmmmm ......nnnnnddddd -- SVE mixed sign dot product
        case IF_SVE_GJ_3A:   // ...........mmmmm ......nnnnnddddd -- SVE2 crypto constructive binary operations
        case IF_SVE_GN_3A:   // ...........mmmmm ......nnnnnddddd -- SVE2 FP8 multiply-add long
        case IF_SVE_GO_3A:   // ...........mmmmm ......nnnnnddddd -- SVE2 FP8 multiply-add long long
        case IF_SVE_GW_3B:   // ...........mmmmm ......nnnnnddddd -- SVE FP clamp
        case IF_SVE_HA_3A:   // ...........mmmmm ......nnnnnddddd -- SVE BFloat16 floating-point dot product
        case IF_SVE_HA_3A_E: // ...........mmmmm ......nnnnnddddd -- SVE BFloat16 floating-point dot product
        case IF_SVE_HB_3A:   // ...........mmmmm ......nnnnnddddd -- SVE floating-point multiply-add long
        case IF_SVE_HD_3A:   // ...........mmmmm ......nnnnnddddd -- SVE floating point matrix multiply accumulate
        case IF_SVE_HD_3A_A: // ...........mmmmm ......nnnnnddddd -- SVE floating point matrix multiply accumulate
        case IF_SVE_HK_3B:   // ...........mmmmm ......nnnnnddddd -- SVE floating-point arithmetic (unpredicated)
        case IF_SVE_AV_3A:   // ...........mmmmm ......kkkkkddddd -- SVE2 bitwise ternary operations
            assert(insOptsScalable(id->idInsOpt()));
            assert(isVectorRegister(id->idReg1())); // ddddd
            assert(isVectorRegister(id->idReg2())); // nnnnn/mmmmm
            assert(isVectorRegister(id->idReg3())); // mmmmm/aaaaa
            break;
        case IF_SVE_AU_3A: // ...........mmmmm ......nnnnnddddd -- SVE bitwise logical operations (unpredicated)
            assert(insOptsScalable(id->idInsOpt()));
            assert(isVectorRegister(id->idReg1()));                                 // ddddd
            assert(isVectorRegister(id->idReg2()));                                 // nnnnn/mmmmm
            assert((id->idIns() == INS_sve_mov) || isVectorRegister(id->idReg3())); // mmmmm/aaaaa
            break;

        case IF_SVE_HA_3A_F: // ...........mmmmm ......nnnnnddddd -- SVE BFloat16 floating-point dot product
        case IF_SVE_EW_3A:   // ...........mmmmm ......nnnnnddddd -- SVE2 multiply-add (checked pointer)
        case IF_SVE_EW_3B:   // ...........mmmmm ......aaaaaddddd -- SVE2 multiply-add (checked pointer)
            assert(insOptsNone(id->idInsOpt()));
            assert(isVectorRegister(id->idReg1())); // ddddd
            assert(isVectorRegister(id->idReg2())); // nnnnn/aaaaa
            assert(isVectorRegister(id->idReg3())); // mmmmm
            break;

        case IF_SVE_EG_3A:   // ...........iimmm ......nnnnnddddd -- SVE two-way dot product (indexed)
        case IF_SVE_EY_3A:   // ...........iimmm ......nnnnnddddd -- SVE integer dot product (indexed)
        case IF_SVE_EZ_3A:   // ...........iimmm ......nnnnnddddd -- SVE mixed sign dot product (indexed)
        case IF_SVE_FD_3B:   // ...........iimmm ......nnnnnddddd -- SVE2 integer multiply (indexed)
        case IF_SVE_FF_3B:   // ...........iimmm ......nnnnnddddd -- SVE2 integer multiply-add (indexed)
        case IF_SVE_FI_3B:   // ...........iimmm ......nnnnnddddd -- SVE2 saturating multiply high (indexed)
        case IF_SVE_GU_3A:   // ...........iimmm ......nnnnnddddd -- SVE floating-point multiply-add (indexed)
        case IF_SVE_GX_3A:   // ...........iimmm ......nnnnnddddd -- SVE floating-point multiply (indexed)
        case IF_SVE_GY_3B:   // ...........iimmm ......nnnnnddddd -- SVE BFloat16 floating-point dot product (indexed)
        case IF_SVE_GY_3B_D: // ...........iimmm ......nnnnnddddd -- SVE BFloat16 floating-point dot product (indexed)
        case IF_SVE_FK_3B:   // ...........iimmm ......nnnnnddddd -- SVE2 saturating multiply-add high (indexed)
            assert(insOptsScalableStandard(id->idInsOpt()));
            assert(isVectorRegister(id->idReg1())); // ddddd
            assert(isVectorRegister(id->idReg2())); // nnnnn
            assert(isVectorRegister(id->idReg3())); // mmm
            assert((REG_V0 <= id->idReg3()) && (id->idReg3() <= REG_V7));
            assert(isValidUimm<2>(emitGetInsSC(id))); // ii
            break;

        case IF_SVE_FD_3A: // .........i.iimmm ......nnnnnddddd -- SVE2 integer multiply (indexed)
        case IF_SVE_FE_3A: // ...........iimmm ....i.nnnnnddddd -- SVE2 integer multiply long (indexed)
        case IF_SVE_FF_3A: // .........i.iimmm ......nnnnnddddd -- SVE2 integer multiply-add (indexed)
        case IF_SVE_FG_3A: // ...........iimmm ....i.nnnnnddddd -- SVE2 integer multiply-add long (indexed)
        case IF_SVE_FH_3A: // ...........iimmm ....i.nnnnnddddd -- SVE2 saturating multiply (indexed)
        case IF_SVE_FI_3A: // .........i.iimmm ......nnnnnddddd -- SVE2 saturating multiply high (indexed)
        case IF_SVE_FJ_3A: // ...........iimmm ....i.nnnnnddddd -- SVE2 saturating multiply-add (indexed)
        case IF_SVE_FK_3A: // .........i.iimmm ......nnnnnddddd -- SVE2 saturating multiply-add high (indexed)
        case IF_SVE_GU_3C: // .........i.iimmm ......nnnnnddddd -- SVE floating-point multiply-add (indexed)
        case IF_SVE_GX_3C: // .........i.iimmm ......nnnnnddddd -- SVE floating-point multiply (indexed)
        case IF_SVE_GY_3A: // ...........iimmm ....i.nnnnnddddd -- SVE BFloat16 floating-point dot product (indexed)
        case IF_SVE_GZ_3A: // ...........iimmm ....i.nnnnnddddd -- SVE floating-point multiply-add long (indexed)
            assert(insOptsScalableStandard(id->idInsOpt()));
            assert(isVectorRegister(id->idReg1())); // ddddd
            assert(isVectorRegister(id->idReg2())); // nnnnn
            assert(isVectorRegister(id->idReg3())); // mmm
            assert((REG_V0 <= id->idReg3()) && (id->idReg3() <= REG_V7));
            assert(isValidUimm<3>(emitGetInsSC(id))); // iii
            break;

        case IF_SVE_FE_3B: // ...........immmm ....i.nnnnnddddd -- SVE2 integer multiply long (indexed)
        case IF_SVE_FG_3B: // ...........immmm ....i.nnnnnddddd -- SVE2 integer multiply-add long (indexed)
        case IF_SVE_FH_3B: // ...........immmm ....i.nnnnnddddd -- SVE2 saturating multiply (indexed)
        case IF_SVE_FJ_3B: // ...........immmm ....i.nnnnnddddd -- SVE2 saturating multiply-add (indexed)
            assert(id->idInsOpt() == INS_OPTS_SCALABLE_S);
            assert(isVectorRegister(id->idReg1()));    // ddddd
            assert(isVectorRegister(id->idReg2()));    // nnnnn
            assert(isLowVectorRegister(id->idReg3())); // mmmm
            assert(isValidUimm<2>(emitGetInsSC(id)));  // ii
            break;

        case IF_SVE_EY_3B: // ...........immmm ......nnnnnddddd -- SVE integer dot product (indexed)
        case IF_SVE_FD_3C: // ...........immmm ......nnnnnddddd -- SVE2 integer multiply (indexed)
        case IF_SVE_FF_3C: // ...........immmm ......nnnnnddddd -- SVE2 integer multiply-add (indexed)
        case IF_SVE_FI_3C: // ...........immmm ......nnnnnddddd -- SVE2 saturating multiply high (indexed)
        case IF_SVE_GU_3B: // ...........immmm ......nnnnnddddd -- SVE floating-point multiply-add (indexed)
        case IF_SVE_GX_3B: // ...........immmm ......nnnnnddddd -- SVE floating-point multiply (indexed)
        case IF_SVE_FK_3C: // ...........immmm ......nnnnnddddd -- SVE2 saturating multiply-add high (indexed)
            assert(insOptsScalableStandard(id->idInsOpt()));
            assert(isVectorRegister(id->idReg1()));    // ddddd
            assert(isVectorRegister(id->idReg2()));    // nnnnn
            assert(isLowVectorRegister(id->idReg3())); // mmmm
            assert(isValidUimm<1>(emitGetInsSC(id)));  // i
            break;

        case IF_SVE_CZ_4A: // ............MMMM ..gggg.NNNN.DDDD -- SVE predicate logical operations
            assert(id->idInsOpt() == INS_OPTS_SCALABLE_B);
            assert(isPredicateRegister(id->idReg1())); // DDDD
            assert(isPredicateRegister(id->idReg2())); // gggg
            assert(isPredicateRegister(id->idReg3())); // NNNN

            switch (id->idIns())
            {
                case INS_sve_and:
                case INS_sve_ands:
                case INS_sve_bic:
                case INS_sve_bics:
                case INS_sve_eor:
                case INS_sve_eors:
                case INS_sve_nand:
                case INS_sve_nands:
                case INS_sve_nor:
                case INS_sve_nors:
                case INS_sve_orn:
                case INS_sve_orns:
                case INS_sve_orr:
                case INS_sve_orrs:
                case INS_sve_sel:
                    assert(isPredicateRegister(id->idReg4())); // MMMM
                    break;

                case INS_sve_mov:
                case INS_sve_movs:
                case INS_sve_not:
                case INS_sve_nots:
                    // no fourth register
                    break;

                default:
                    unreached();
                    break;
            }
            break;

        case IF_SVE_CZ_4A_A: // ............MMMM ..gggg.NNNN.DDDD -- SVE predicate logical operations
        case IF_SVE_CZ_4A_L: // ............MMMM ..gggg.NNNN.DDDD -- SVE predicate logical operations
            assert(id->idInsOpt() == INS_OPTS_SCALABLE_B);
            assert(isPredicateRegister(id->idReg1())); // DDDD
            assert(isPredicateRegister(id->idReg2())); // NNNN
            break;

        case IF_SVE_CZ_4A_K: // ............MMMM ..gggg.NNNN.DDDD -- SVE predicate logical operations
        case IF_SVE_DB_3A:   // ................ ..gggg.NNNNMDDDD -- SVE partition break condition
        case IF_SVE_DB_3B:   // ................ ..gggg.NNNN.DDDD -- SVE partition break condition
        case IF_SVE_DC_3A:   // ................ ..gggg.NNNN.MMMM -- SVE propagate break to next partition
            assert(isScalableVectorSize(id->idOpSize()));
            assert(id->idInsOpt() == INS_OPTS_SCALABLE_B);
            assert(isPredicateRegister(id->idReg1())); // DDDD
            assert(isPredicateRegister(id->idReg2())); // gggg
            assert(isPredicateRegister(id->idReg3())); // NNNN
            break;

        case IF_SVE_DA_4A: // ............MMMM ..gggg.NNNN.DDDD -- SVE propagate break from previous partition
            assert(isScalableVectorSize(id->idOpSize()));
            assert(id->idInsOpt() == INS_OPTS_SCALABLE_B);
            assert(isPredicateRegister(id->idReg1())); // DDDD
            assert(isPredicateRegister(id->idReg2())); // gggg
            assert(isPredicateRegister(id->idReg3())); // NNNN
            assert(isPredicateRegister(id->idReg4())); // MMMM
            break;

        case IF_SVE_DD_2A: // ................ .......gggg.DDDD -- SVE predicate first active
        case IF_SVE_DG_2A: // ................ .......gggg.DDDD -- SVE predicate read from FFR (predicated)
            assert(id->idInsOpt() == INS_OPTS_SCALABLE_B);
            assert(isPredicateRegister(id->idReg1())); // DDDD
            assert(isPredicateRegister(id->idReg2())); // gggg
            break;

        case IF_SVE_DE_1A: // ........xx...... ......ppppp.DDDD -- SVE predicate initialize
            assert(isScalableVectorSize(id->idOpSize()));
            assert(isPredicateRegister(id->idReg1()));       // DDDD
            assert(insOptsScalableStandard(id->idInsOpt())); // xx
            break;

        case IF_SVE_DF_2A: // ........xx...... .......VVVV.DDDD -- SVE predicate next active
        case IF_SVE_DI_2A: // ................ ..gggg.NNNN..... -- SVE predicate test
            assert(isScalableVectorSize(id->idOpSize()));
            assert(insOptsScalableStandard(id->idInsOpt()));
            assert(isPredicateRegister(id->idReg1())); // DDDD
            assert(isPredicateRegister(id->idReg2())); // gggg
            break;

        case IF_SVE_DH_1A: // ................ ............DDDD -- SVE predicate read from FFR (unpredicated)
        case IF_SVE_DJ_1A: // ................ ............DDDD -- SVE predicate zero
            assert(isScalableVectorSize(id->idOpSize()));
            assert(id->idInsOpt() == INS_OPTS_SCALABLE_B);
            assert(isPredicateRegister(id->idReg1())); // DDDD
            break;

        case IF_SVE_DK_3A: // ........xx...... ..gggg.NNNNddddd -- SVE predicate count
            assert(isScalableVectorSize(id->idOpSize()));
            assert(insOptsScalableStandard(id->idInsOpt()));
            assert(isGeneralRegister(id->idReg1()));   // ddddd
            assert(isPredicateRegister(id->idReg2())); // gggg
            assert(isPredicateRegister(id->idReg3())); // NNNN
            break;

        case IF_SVE_GE_4A: // ........xx.mmmmm ...gggnnnnn.DDDD -- SVE2 character match
            assert(isScalableVectorSize(id->idOpSize()));
            assert(insOptsScalableAtMaxHalf(id->idInsOpt()));
            assert(isPredicateRegister(id->idReg1()));    // DDDD
            assert(isLowPredicateRegister(id->idReg2())); // ggg
            assert(isVectorRegister(id->idReg3()));       // nnnnn
            assert(isVectorRegister(id->idReg4()));       // mmmmm
            break;

        case IF_SVE_GQ_3A: // ................ ...gggnnnnnddddd -- SVE floating-point convert precision odd elements
            switch (id->idIns())
            {
                case INS_sve_fcvtnt:
                case INS_sve_fcvtlt:
                    assert(insOptsConvertFloatStepwise(id->idInsOpt()));
                    FALLTHROUGH;
                case INS_sve_fcvtxnt:
                case INS_sve_bfcvtnt:
                    assert(isVectorRegister(id->idReg1()));       // ddddd
                    assert(isLowPredicateRegister(id->idReg2())); // ggg
                    assert(isVectorRegister(id->idReg3()));       // nnnnn
                    break;
                default:
                    assert(!"unreachable");
                    break;
            }
            break;

        case IF_SVE_HO_3A: // ................ ...gggnnnnnddddd -- SVE floating-point convert precision
            assert(id->idInsOpt() == INS_OPTS_S_TO_H);
            assert(isVectorRegister(id->idReg1()));       // ddddd
            assert(isLowPredicateRegister(id->idReg2())); // ggg
            assert(isVectorRegister(id->idReg3()));       // nnnnn
            break;

        case IF_SVE_HO_3B:
            assert(insOptsConvertFloatToFloat(id->idInsOpt()));
            assert(isVectorRegister(id->idReg1()));       // ddddd
            assert(isLowPredicateRegister(id->idReg2())); // ggg
            assert(isVectorRegister(id->idReg3()));       // nnnnn
            break;

        case IF_SVE_HO_3C:
            assert(id->idInsOpt() == INS_OPTS_D_TO_S);
            assert(isVectorRegister(id->idReg1()));       // ddddd
            assert(isLowPredicateRegister(id->idReg2())); // ggg
            assert(isVectorRegister(id->idReg3()));       // nnnnn
            break;

        case IF_SVE_HP_3B: // ................ ...gggnnnnnddddd -- SVE floating-point convert to integer
            assert(insOptsScalableFloat(id->idInsOpt()) || id->idInsOpt() == INS_OPTS_H_TO_S ||
                   id->idInsOpt() == INS_OPTS_H_TO_D || id->idInsOpt() == INS_OPTS_S_TO_D ||
                   id->idInsOpt() == INS_OPTS_D_TO_S);
            assert(isVectorRegister(id->idReg1()));       // ddddd
            assert(isLowPredicateRegister(id->idReg2())); // ggg
            assert(isVectorRegister(id->idReg3()));       // nnnnn
            break;

        case IF_SVE_HS_3A: // ................ ...gggnnnnnddddd -- SVE integer convert to floating-point
            assert(insOptsScalableAtLeastHalf(id->idInsOpt()) || id->idInsOpt() == INS_OPTS_S_TO_H ||
                   id->idInsOpt() == INS_OPTS_S_TO_D || id->idInsOpt() == INS_OPTS_D_TO_H ||
                   id->idInsOpt() == INS_OPTS_D_TO_S);
            assert(isVectorRegister(id->idReg1()));       // ddddd
            assert(isLowPredicateRegister(id->idReg2())); // ggg
            assert(isVectorRegister(id->idReg3()));       // nnnnn
            break;

        case IF_SVE_HT_4A: // ........xx.mmmmm ...gggnnnnn.DDDD -- SVE floating-point compare vectors
            assert(isScalableVectorSize(id->idOpSize()));
            assert(insOptsScalableFloat(id->idInsOpt()));
            assert(isPredicateRegister(id->idReg1()));    // DDDD
            assert(isLowPredicateRegister(id->idReg2())); // ggg
            assert(isVectorRegister(id->idReg3()));       // nnnnn
            assert(isVectorRegister(id->idReg4()));       // mmmmm
            break;

        // Scalable FP.
        case IF_SVE_GR_3A: // ........xx...... ...gggmmmmmddddd -- SVE2 floating-point pairwise operations
        case IF_SVE_HL_3A: // ........xx...... ...gggmmmmmddddd -- SVE floating-point arithmetic (predicated)
        case IF_SVE_HR_3A: // ........xx...... ...gggnnnnnddddd -- SVE floating-point unary operations
            assert(insOptsScalableFloat(id->idInsOpt())); // xx
            assert(isVectorRegister(id->idReg1()));       // ddddd
            assert(isLowPredicateRegister(id->idReg2())); // ggg
            assert(isVectorRegister(id->idReg3()));       // mmmmm
            assert(isScalableVectorSize(id->idOpSize()));
            break;

        case IF_SVE_AB_3B: // ................ ...gggmmmmmddddd -- SVE integer add/subtract vectors (predicated)
        case IF_SVE_HL_3B: // ................ ...gggmmmmmddddd -- SVE floating-point arithmetic (predicated)
            assert(insOptsScalableStandard(id->idInsOpt()));
            assert(isVectorRegister(id->idReg1()));       // ddddd
            assert(isLowPredicateRegister(id->idReg2())); // ggg
            assert(isVectorRegister(id->idReg3()));       // mmmmm
            assert(isScalableVectorSize(id->idOpSize()));
            break;

        // Scalable to Simd Vector.
        case IF_SVE_AG_3A: // ........xx...... ...gggnnnnnddddd -- SVE bitwise logical reduction (quadwords)
        case IF_SVE_AJ_3A: // ........xx...... ...gggnnnnnddddd -- SVE integer add reduction (quadwords)
        case IF_SVE_AL_3A: // ........xx...... ...gggnnnnnddddd -- SVE integer min/max reduction (quadwords)
            assert(insOptsScalableStandard(id->idInsOpt())); // xx
            assert(isVectorRegister(id->idReg1()));          // ddddd
            assert(isLowPredicateRegister(id->idReg2()));    // ggg
            assert(isVectorRegister(id->idReg3()));          // mmmmm
            assert(id->idOpSize() == EA_8BYTE);
            break;

        // Scalable FP to Simd Vector.
        case IF_SVE_GS_3A: // ........xx...... ...gggnnnnnddddd -- SVE floating-point recursive reduction (quadwords)
            assert(insOptsScalableFloat(id->idInsOpt())); // xx
            assert(isVectorRegister(id->idReg1()));       // ddddd
            assert(isLowPredicateRegister(id->idReg2())); // ggg
            assert(isVectorRegister(id->idReg3()));       // mmmmm
            assert(id->idOpSize() == EA_8BYTE);
            break;

        // Scalable, widening to scalar SIMD.
        case IF_SVE_AI_3A: // ........xx...... ...gggnnnnnddddd -- SVE integer add reduction (predicated)
            switch (id->idIns())
            {
                case INS_sve_saddv:
                    assert(insOptsScalableWide(id->idInsOpt())); // xx
                    break;

                default:
                    assert(insOptsScalableStandard(id->idInsOpt())); // xx
                    break;
            }
            assert(isVectorRegister(id->idReg1()));       // ddddd
            assert(isLowPredicateRegister(id->idReg2())); // ggg
            assert(isVectorRegister(id->idReg3()));       // mmmmm
            assert(isScalableVectorSize(id->idOpSize()));
            break;

        // Scalable, possibly FP.
        case IF_SVE_AP_3A: // ........xx...... ...gggnnnnnddddd -- SVE bitwise unary operations (predicated)
            switch (id->idIns())
            {
                case INS_sve_fabs:
                case INS_sve_fneg:
                    assert(insOptsScalableFloat(id->idInsOpt())); // xx
                    break;

                default:
                    assert(insOptsScalableStandard(id->idInsOpt())); // xx
                    break;
            }
            assert(isVectorRegister(id->idReg1()));       // ddddd
            assert(isLowPredicateRegister(id->idReg2())); // ggg
            assert(isVectorRegister(id->idReg3()));       // mmmmm
            assert(isScalableVectorSize(id->idOpSize()));
            break;

        // Scalable, various sizes.
        case IF_SVE_AQ_3A: // ........xx...... ...gggnnnnnddddd -- SVE integer unary operations (predicated)
        case IF_SVE_CU_3A: // ........xx...... ...gggnnnnnddddd -- SVE reverse within elements
            switch (id->idIns())
            {
                case INS_sve_abs:
                case INS_sve_neg:
                case INS_sve_rbit:
                    assert(insOptsScalableStandard(id->idInsOpt()));
                    break;

                case INS_sve_sxtb:
                case INS_sve_uxtb:
                case INS_sve_revb:
                    assert(insOptsScalableAtLeastHalf(id->idInsOpt()));
                    break;

                case INS_sve_sxth:
                case INS_sve_uxth:
                case INS_sve_revh:
                    assert(insOptsScalableWords(id->idInsOpt()));
                    break;

                default:
                    assert(id->idInsOpt() == INS_OPTS_SCALABLE_D);
                    break;
            }
            assert(isVectorRegister(id->idReg1()));       // ddddd
            assert(isLowPredicateRegister(id->idReg2())); // ggg
            assert(isVectorRegister(id->idReg3()));       // mmmmm
            assert(isScalableVectorSize(id->idOpSize()));
            break;

        case IF_SVE_CV_3A: // ........xx...... ...VVVnnnnnddddd -- SVE vector splice (constructive)
        case IF_SVE_CV_3B: // ........xx...... ...VVVmmmmmddddd -- SVE vector splice (destructive)
            assert(isScalableVectorSize(id->idOpSize())); // xx
            assert(insOptsScalableStandard(id->idInsOpt()));
            assert(isVectorRegister(id->idReg1()));       // ddddd
            assert(isLowPredicateRegister(id->idReg2())); // VVV
            assert(isVectorRegister(id->idReg3()));       // nnnnn
            break;

        case IF_SVE_CW_4A: // ........xx.mmmmm ..VVVVnnnnnddddd -- SVE select vector elements (predicated)
            assert(isScalableVectorSize(id->idOpSize())); // xx
            assert(insOptsScalableStandard(id->idInsOpt()));
            assert(isVectorRegister(id->idReg1()));    // ddddd
            assert(isPredicateRegister(id->idReg2())); // VVVV
            assert(isVectorRegister(id->idReg3()));    // nnnnn
            if (id->idIns() == INS_sve_sel)
            {
                assert(isVectorRegister(id->idReg4())); // mmmmm
            }
            break;

        // Scalable from general scalar (possibly SP)
        case IF_SVE_CQ_3A: // ........xx...... ...gggnnnnnddddd -- SVE copy general register to vector (predicated)
            assert(insOptsScalableStandard(id->idInsOpt())); // xx
            assert(isVectorRegister(id->idReg1()));          // ddddd
            assert(isLowPredicateRegister(id->idReg2()));    // ggg
            assert(isGeneralRegisterOrZR(id->idReg3()));     // mmmmm
            assert(isValidScalarDatasize(id->idOpSize()));
            break;

        // Scalable, .H, .S or .D
        case IF_SVE_EQ_3A: // ........xx...... ...gggnnnnnddddd -- SVE2 integer pairwise add and accumulate long
        case IF_SVE_HQ_3A: // ........xx...... ...gggnnnnnddddd -- SVE floating-point round to integral value
            assert(insOptsScalableAtLeastHalf(id->idInsOpt())); // xx
            assert(isVectorRegister(id->idReg1()));             // ddddd
            assert(isLowPredicateRegister(id->idReg2()));       // ggg
            assert(isVectorRegister(id->idReg3()));             // mmmmm
            assert(isScalableVectorSize(id->idOpSize()));
            break;

        // Scalable, possibly fixed to .S
        case IF_SVE_ES_3A: // ........xx...... ...gggnnnnnddddd -- SVE2 integer unary operations (predicated)
            switch (id->idIns())
            {
                case INS_sve_sqabs:
                case INS_sve_sqneg:
                    assert(insOptsScalableStandard(id->idInsOpt()));
                    break;

                default:
                    assert(id->idInsOpt() == INS_OPTS_SCALABLE_S);
                    break;
            }
            assert(isVectorRegister(id->idReg1()));       // ddddd
            assert(isLowPredicateRegister(id->idReg2())); // ggg
            assert(isVectorRegister(id->idReg3()));       // mmmmm
            assert(isScalableVectorSize(id->idOpSize()));
            break;

        case IF_SVE_GA_2A: // ............iiii ......nnnn.ddddd -- SME2 multi-vec shift narrow
            assert(id->idInsOpt() == INS_OPTS_SCALABLE_H);
            assert(isVectorRegister(id->idReg1())); // nnnn
            assert(isVectorRegister(id->idReg2())); // ddddd
            assert(isEvenRegister(id->idReg2()));
            assert(isScalableVectorSize(id->idOpSize()));
            break;

        case IF_SVE_DL_2A: // ........xx...... .....l.NNNNddddd -- SVE predicate count (predicate-as-counter)
            assert(insOptsScalableStandard(id->idInsOpt()));
            assert(isValidVectorElemsize(optGetSveElemsize(id->idInsOpt()))); // xx
            assert(isGeneralRegister(id->idReg1()));                          // ddddd
            assert(isPredicateRegister(id->idReg2()));                        // NNNN
            assert(isScalableVectorSize(id->idOpSize()));
            break;

        case IF_SVE_DO_2A: // ........xx...... .....X.MMMMddddd -- SVE saturating inc/dec register by predicate count
        case IF_SVE_DM_2A: // ........xx...... .......MMMMddddd -- SVE inc/dec register by predicate count
            assert(insOptsScalableStandard(id->idInsOpt()));
            assert(isValidVectorElemsize(optGetSveElemsize(id->idInsOpt()))); // xx
            assert(isGeneralRegister(id->idReg1()));                          // ddddd
            assert(isPredicateRegister(id->idReg2()));                        // MMMM
            assert(isValidGeneralDatasize(id->idOpSize()));
            break;

        case IF_SVE_DP_2A: // ........xx...... .......MMMMddddd -- SVE saturating inc/dec vector by predicate count
        case IF_SVE_DN_2A: // ........xx...... .......MMMMddddd -- SVE inc/dec vector by predicate count
            assert(insOptsScalableAtLeastHalf(id->idInsOpt())); // xx
            assert(isVectorRegister(id->idReg1()));             // ddddd
            assert(isPredicateRegister(id->idReg2()));          // MMMM
            assert(isScalableVectorSize(id->idOpSize()));
            break;

        case IF_SVE_DQ_0A: // ................ ................ -- SVE FFR initialise
            break;

        case IF_SVE_DR_1A: // ................ .......NNNN..... -- SVE FFR write from predicate
            assert(id->idInsOpt() == INS_OPTS_SCALABLE_B);
            assert(isPredicateRegister(id->idReg1())); // NNNN
            break;

        case IF_SVE_DS_2A: // .........x.mmmmm ......nnnnn..... -- SVE conditionally terminate scalars
            assert(insOptsNone(id->idInsOpt()));
            assert(isGeneralRegister(id->idReg1()));        // nnnnn
            assert(isGeneralRegister(id->idReg2()));        // mmmmm
            assert(isValidGeneralDatasize(id->idOpSize())); // x
            break;

        case IF_SVE_FZ_2A: // ................ ......nnnn.ddddd -- SME2 multi-vec extract narrow
            assert(insOptsNone(id->idInsOpt()));
            assert(isVectorRegister(id->idReg1())); // ddddd
            assert(isVectorRegister(id->idReg2())); // nnnn
            assert(isEvenRegister(id->idReg2()));
            break;

        case IF_SVE_HG_2A: // ................ ......nnnn.ddddd -- SVE2 FP8 downconverts
            assert(insOptsNone(id->idInsOpt()));
            assert(isVectorRegister(id->idReg1())); // ddddd
            assert(isVectorRegister(id->idReg2())); // nnnn
            assert(isEvenRegister(id->idReg2()));
            break;

        case IF_SVE_GD_2A: // .........x.xx... ......nnnnnddddd -- SVE2 saturating extract narrow
            assert(insOptsScalableStandard(id->idInsOpt()));
            assert(isVectorRegister(id->idReg1())); // nnnnn
            assert(isVectorRegister(id->idReg2())); // ddddd
            assert(optGetSveElemsize(id->idInsOpt()) != EA_8BYTE);
            assert(isValidVectorElemsize(optGetSveElemsize(id->idInsOpt()))); // xx
                                                                              // x
            break;

        case IF_SVE_BB_2A: // ...........nnnnn .....iiiiiiddddd -- SVE stack frame adjustment
            assert(insOptsNone(id->idInsOpt()));
            assert(id->idOpSize() == EA_8BYTE);
            assert(isGeneralRegisterOrZR(id->idReg1())); // ddddd
            assert(isGeneralRegisterOrZR(id->idReg2())); // nnnnn
            assert(isValidSimm<6>(emitGetInsSC(id)));    // iiiiii
            break;

        case IF_SVE_BC_1A: // ................ .....iiiiiiddddd -- SVE stack frame size
            assert(insOptsNone(id->idInsOpt()));
            assert(id->idOpSize() == EA_8BYTE);
            assert(isGeneralRegister(id->idReg1()));  // ddddd
            assert(isValidSimm<6>(emitGetInsSC(id))); // iiiiii
            break;

        case IF_SVE_AW_2A: // ........xx.xxiii ......mmmmmddddd -- sve_int_rotate_imm
        {
            assert(insOptsScalableStandard(id->idInsOpt()));
            assert(isVectorRegister(id->idReg1()));                           // ddddd
            assert(isVectorRegister(id->idReg2()));                           // mmmmm
            assert(isValidVectorElemsize(optGetSveElemsize(id->idInsOpt()))); // xx xx
            imm = emitGetInsSC(id);

            switch (id->idInsOpt())
            {
                case INS_OPTS_SCALABLE_B:
                    assert(isValidUimmFrom1<3>(imm)); // iii
                    break;

                case INS_OPTS_SCALABLE_H:
                    assert(isValidUimmFrom1<4>(imm)); // xiii
                    break;

                case INS_OPTS_SCALABLE_S:
                    assert(isValidUimmFrom1<5>(imm)); // xxiii
                    break;

                case INS_OPTS_SCALABLE_D:
                    assert(isValidUimmFrom1<6>(imm)); // xx xiii
                    break;

                default:
                    unreached();
                    break;
            }
            break;
        }

        case IF_SVE_AX_1A: // ........xx.iiiii ......iiiiiddddd -- SVE index generation (immediate start, immediate
                           // increment)
        {
            ssize_t imm1;
            ssize_t imm2;
            insSveDecodeTwoSimm5(emitGetInsSC(id), &imm1, &imm2);
            assert(insOptsScalableStandard(id->idInsOpt()));
            assert(isVectorRegister(id->idReg1()));                           // ddddd
            assert(isValidSimm<5>(imm1));                                     // iiiii
            assert(isValidSimm<5>(imm2));                                     // iiiii
            assert(isValidVectorElemsize(optGetSveElemsize(id->idInsOpt()))); // xx
            break;
        }

        case IF_SVE_AY_2A: // ........xx.mmmmm ......iiiiiddddd -- SVE index generation (immediate start, register
                           // increment)
        case IF_SVE_AZ_2A: // ........xx.iiiii ......nnnnnddddd -- SVE index generation (register start, immediate
                           // increment)
            assert(insOptsScalableStandard(id->idInsOpt()));
            assert(isVectorRegister(id->idReg1()));                           // ddddd
            assert(isValidSimm<5>(emitGetInsSC(id)));                         // iiiii
            assert(isIntegerRegister(id->idReg2()));                          // mmmmm
            assert(isValidVectorElemsize(optGetSveElemsize(id->idInsOpt()))); // xx
            break;

        case IF_SVE_FR_2A: // .........x.xxiii ......nnnnnddddd -- SVE2 bitwise shift left long
        {
            assert(insOptsScalableWide(id->idInsOpt()));
            assert(isVectorRegister(id->idReg1()));                           // ddddd
            assert(isVectorRegister(id->idReg2()));                           // nnnnn
            assert(isValidVectorElemsize(optGetSveElemsize(id->idInsOpt()))); // x xx
            imm = emitGetInsSC(id);

            switch (id->idInsOpt())
            {
                case INS_OPTS_SCALABLE_B:
                    assert(isValidUimm<3>(imm)); // iii
                    break;

                case INS_OPTS_SCALABLE_H:
                    assert(isValidUimm<4>(imm)); // x iii
                    break;

                case INS_OPTS_SCALABLE_S:
                    assert(isValidUimm<5>(imm)); // xx iii
                    break;

                default:
                    unreached();
                    break;
            }
            break;
        }

        case IF_SVE_GB_2A: // .........x.xxiii ......nnnnnddddd -- SVE2 bitwise shift right narrow
        {
            assert(insOptsScalableWide(id->idInsOpt()));
            assert(isVectorRegister(id->idReg1()));                           // ddddd
            assert(isVectorRegister(id->idReg2()));                           // nnnnn
            assert(isValidVectorElemsize(optGetSveElemsize(id->idInsOpt()))); // x xx
            imm = emitGetInsSC(id);

            switch (id->idInsOpt())
            {
                case INS_OPTS_SCALABLE_B:
                    assert(isValidUimmFrom1<3>(imm)); // iii
                    break;

                case INS_OPTS_SCALABLE_H:
                    assert(isValidUimmFrom1<4>(imm)); // x iii
                    break;

                case INS_OPTS_SCALABLE_S:
                    assert(isValidUimmFrom1<5>(imm)); // xx iii
                    break;

                default:
                    unreached();
                    break;
            }
            break;
        }

        case IF_SVE_FV_2A: // ........xx...... .....rmmmmmddddd -- SVE2 complex integer add
            assert(insOptsScalableStandard(id->idInsOpt()));
            assert(isVectorRegister(id->idReg1()));                           // ddddd
            assert(isVectorRegister(id->idReg2()));                           // nnnnn
            assert(emitIsValidEncodedRotationImm90_or_270(emitGetInsSC(id))); // r
            assert(isValidVectorElemsize(optGetSveElemsize(id->idInsOpt()))); // xx
            break;

        case IF_SVE_FY_3A: // .........x.mmmmm ......nnnnnddddd -- SVE2 integer add/subtract long with carry
            assert(insOptsScalableWords(id->idInsOpt()));
            assert(isVectorRegister(id->idReg1()));                           // ddddd
            assert(isVectorRegister(id->idReg2()));                           // nnnnn
            assert(isVectorRegister(id->idReg3()));                           // mmmmm
            assert(isValidVectorElemsize(optGetSveElemsize(id->idInsOpt()))); // x
            break;

        case IF_SVE_GK_2A: // ................ ......mmmmmddddd -- SVE2 crypto destructive binary operations
            assert(insOptsScalableStandard(id->idInsOpt()));
            assert(isVectorRegister(id->idReg1())); // ddddd
            assert(isVectorRegister(id->idReg2())); // mmmmm
            if (id->idInsOpt() == INS_OPTS_SCALABLE_S)
            {
                assert(id->idIns() == INS_sve_sm4e);
            }
            else
            {
                assert(id->idInsOpt() == INS_OPTS_SCALABLE_B);
            }
            assert(isScalableVectorSize(id->idOpSize()));
            break;

        case IF_SVE_GL_1A: // ................ ...........ddddd -- SVE2 crypto unary operations
            assert(insOptsScalableStandard(id->idInsOpt()));
            assert(isVectorRegister(id->idReg1())); // ddddd
            assert(isScalableVectorSize(id->idOpSize()));
            break;

        case IF_SVE_DU_3A: // ........xx.mmmmm ......nnnnn.DDDD -- SVE pointer conflict compare
            assert(id->idOpSize() == EA_8BYTE);

            FALLTHROUGH;
        case IF_SVE_DT_3A: // ........xx.mmmmm ...X..nnnnn.DDDD -- SVE integer compare scalar count and limit
            assert(insOptsScalableStandard(id->idInsOpt()));
            assert(isPredicateRegister(id->idReg1()));                        // DDDD
            assert(isGeneralRegister(id->idReg2()));                          // nnnnn
            assert(isValidGeneralDatasize(id->idOpSize()));                   // X
            assert(isGeneralRegister(id->idReg3()));                          // mmmmm
            assert(isValidVectorElemsize(optGetSveElemsize(id->idInsOpt()))); // xx
            break;

        case IF_SVE_DV_4A: // ........ix.xxxvv ..NNNN.MMMM.DDDD -- SVE broadcast predicate element
        {
            assert(insOptsScalableStandard(id->idInsOpt()));
            assert(isPredicateRegister(id->idReg1())); // DDDD
            assert(isPredicateRegister(id->idReg2())); // NNNN
            assert(isPredicateRegister(id->idReg3())); // MMMM
            assert(isGeneralRegister(id->idReg4()));   // vv
            assert((REG_R12 <= id->idReg4()) && (id->idReg4() <= REG_R15));
            imm = emitGetInsSC(id);

            switch (id->idInsOpt())
            {
                case INS_OPTS_SCALABLE_B:
                    assert(isValidUimm<4>(imm));
                    break;

                case INS_OPTS_SCALABLE_H:
                    assert(isValidUimm<3>(imm));
                    break;

                case INS_OPTS_SCALABLE_S:
                    assert(isValidUimm<2>(imm));
                    break;

                case INS_OPTS_SCALABLE_D:
                    assert(isValidUimm<1>(imm));
                    break;

                default:
                    unreached();
                    break;
            }

            break;
        }

        case IF_SVE_DW_2B: // ........xx...... .......iNNN.DDDD -- SVE extract mask predicate from predicate-as-counter
            assert(isValidUimm<1>(emitGetInsSC(id))); // i

            FALLTHROUGH;
        case IF_SVE_DW_2A: // ........xx...... ......iiNNN.DDDD -- SVE extract mask predicate from predicate-as-counter
            assert(insOptsScalableStandard(id->idInsOpt()));
            assert(isPredicateRegister(id->idReg1()));                        // DDDD
            assert(isHighPredicateRegister(id->idReg2()));                    // NNN
            assert(isValidUimm<2>(emitGetInsSC(id)));                         // ii
            assert(isValidVectorElemsize(optGetSveElemsize(id->idInsOpt()))); // xx
            break;

        case IF_SVE_DX_3A: // ........xx.mmmmm ......nnnnn.DDD. -- SVE integer compare scalar count and limit (predicate
                           // pair)
            assert(insOptsScalableStandard(id->idInsOpt()));
            assert(isLowPredicateRegister(id->idReg1()));                     // DDD
            assert(isGeneralRegister(id->idReg2()));                          // nnnnn
            assert(isGeneralRegister(id->idReg3()));                          // mmmmm
            assert(isValidVectorElemsize(optGetSveElemsize(id->idInsOpt()))); // xx
            break;

        case IF_SVE_DY_3A: // ........xx.mmmmm ..l...nnnnn..DDD -- SVE integer compare scalar count and limit
                           // (predicate-as-counter)
            assert(insOptsScalableStandard(id->idInsOpt()));                  // L
            assert(isHighPredicateRegister(id->idReg1()));                    // DDD
            assert(isGeneralRegister(id->idReg2()));                          // nnnnn
            assert(isGeneralRegister(id->idReg3()));                          // mmmmm
            assert(isValidVectorElemsize(optGetSveElemsize(id->idInsOpt()))); // xx
            break;

        case IF_SVE_DZ_1A: // ........xx...... .............DDD -- sve_int_pn_ptrue
            assert(insOptsScalableStandard(id->idInsOpt()));
            assert(isHighPredicateRegister(id->idReg1()));                    // DDD
            assert(isValidVectorElemsize(optGetSveElemsize(id->idInsOpt()))); // xx
            break;

        case IF_SVE_EA_1A: // ........xx...... ...iiiiiiiiddddd -- SVE broadcast floating-point immediate (unpredicated)
            assert(insOptsScalableAtLeastHalf(id->idInsOpt()));
            assert(isVectorRegister(id->idReg1()));                           // ddddd
            assert(isValidUimm<8>(emitGetInsSC(id)));                         // iiiiiiii
            assert(isValidVectorElemsize(optGetSveElemsize(id->idInsOpt()))); // xx
            break;

        case IF_SVE_EB_1A: // ........xx...... ..hiiiiiiiiddddd -- SVE broadcast integer immediate (unpredicated)
            imm = emitGetInsSC(id);
            assert(insOptsScalableStandard(id->idInsOpt()));
            assert(isVectorRegister(id->idReg1()));                           // ddddd
            assert(isValidVectorElemsize(optGetSveElemsize(id->idInsOpt()))); // xx
            assert(isValidSimm<8>(imm));                                      // iiiiiiii
            break;

        case IF_SVE_EC_1A: // ........xx...... ..hiiiiiiiiddddd -- SVE integer add/subtract immediate (unpredicated)
            imm = emitGetInsSC(id);
            assert(insOptsScalableStandard(id->idInsOpt()));
            assert(isVectorRegister(id->idReg1()));                           // ddddd
            assert(isValidVectorElemsize(optGetSveElemsize(id->idInsOpt()))); // xx
            assert(isValidUimm<8>(imm));                                      // iiiiiiii
            break;

        case IF_SVE_EB_1B: // ........xx...... ...........ddddd -- SVE broadcast integer immediate (unpredicated)
            assert(insOptsScalableStandard(id->idInsOpt()));
            assert(isVectorRegister(id->idReg1()));                           // ddddd
            assert(isValidVectorElemsize(optGetSveElemsize(id->idInsOpt()))); // xx
            break;

        case IF_SVE_ED_1A: // ........xx...... ...iiiiiiiiddddd -- SVE integer min/max immediate (unpredicated)
            assert(insOptsScalableStandard(id->idInsOpt()));
            assert(isVectorRegister(id->idReg1()));                                       // ddddd
            assert(isValidSimm<8>(emitGetInsSC(id)) || isValidUimm<8>(emitGetInsSC(id))); // iiiiiiii
            assert(isValidVectorElemsize(optGetSveElemsize(id->idInsOpt())));             // xx
            break;

        case IF_SVE_EE_1A: // ........xx...... ...iiiiiiiiddddd -- SVE integer multiply immediate (unpredicated)
            assert(insOptsScalableStandard(id->idInsOpt()));
            assert(isVectorRegister(id->idReg1()));                           // ddddd
            assert(isValidSimm<8>(emitGetInsSC(id)));                         // iiiiiiii
            assert(isValidVectorElemsize(optGetSveElemsize(id->idInsOpt()))); // xx
            break;

        case IF_SVE_EJ_3A: // ........xx.mmmmm ....rrnnnnnddddd -- SVE2 complex integer dot product
            assert(insOptsScalableWords(id->idInsOpt()));

            FALLTHROUGH;
        case IF_SVE_EK_3A: // ........xx.mmmmm ....rrnnnnnddddd -- SVE2 complex integer multiply-add
            assert(insOptsScalableStandard(id->idInsOpt()));
            assert(isVectorRegister(id->idReg1()));                           // ddddd
            assert(isVectorRegister(id->idReg2()));                           // nnnnn
            assert(emitIsValidEncodedRotationImm0_to_270(emitGetInsSC(id)));  // rr
            assert(isVectorRegister(id->idReg3()));                           // mmmmm
            assert(isValidVectorElemsize(optGetSveElemsize(id->idInsOpt()))); // xx
            break;

        case IF_SVE_FA_3A: // ...........iimmm ....rrnnnnnddddd -- SVE2 complex integer dot product (indexed)
        case IF_SVE_FB_3A: // ...........iimmm ....rrnnnnnddddd -- SVE2 complex integer multiply-add (indexed)
        case IF_SVE_FC_3A: // ...........iimmm ....rrnnnnnddddd -- SVE2 complex saturating multiply-add (indexed)
            assert(insOptsScalableStandard(id->idInsOpt()));
            assert(isVectorRegister(id->idReg1())); // ddddd
            assert(isVectorRegister(id->idReg2())); // nnnnn
            assert(isVectorRegister(id->idReg3())); // mmm
            assert((REG_V0 <= id->idReg3()) && (id->idReg3() <= REG_V7));
            assert(isValidUimm<4>(emitGetInsSC(id))); // ii rr
            break;

        case IF_SVE_FA_3B: // ...........immmm ....rrnnnnnddddd -- SVE2 complex integer dot product (indexed)
        case IF_SVE_FB_3B: // ...........immmm ....rrnnnnnddddd -- SVE2 complex integer multiply-add (indexed)
        case IF_SVE_FC_3B: // ...........immmm ....rrnnnnnddddd -- SVE2 complex saturating multiply-add (indexed)
        case IF_SVE_GV_3A: // ...........immmm ....rrnnnnnddddd -- SVE floating-point complex multiply-add (indexed)
            assert(insOptsScalableStandard(id->idInsOpt()));
            assert(isVectorRegister(id->idReg1()));    // ddddd
            assert(isVectorRegister(id->idReg2()));    // nnnnn
            assert(isLowVectorRegister(id->idReg3())); // mmm
            assert(isValidUimm<3>(emitGetInsSC(id)));  // i rr
            break;

        case IF_SVE_IH_3A:   // ............iiii ...gggnnnnnttttt -- SVE contiguous load (quadwords, scalar plus
                             // immediate)
        case IF_SVE_IH_3A_A: // ............iiii ...gggnnnnnttttt -- SVE contiguous load (quadwords, scalar plus
                             // immediate)
        case IF_SVE_IH_3A_F: // ............iiii ...gggnnnnnttttt -- SVE contiguous load (quadwords, scalar plus
                             // immediate)
        case IF_SVE_IJ_3A:   // ............iiii ...gggnnnnnttttt -- SVE contiguous load (scalar plus immediate)
        case IF_SVE_IJ_3A_D: // ............iiii ...gggnnnnnttttt -- SVE contiguous load (scalar plus immediate)
        case IF_SVE_IJ_3A_E: // ............iiii ...gggnnnnnttttt -- SVE contiguous load (scalar plus immediate)
        case IF_SVE_IJ_3A_F: // ............iiii ...gggnnnnnttttt -- SVE contiguous load (scalar plus immediate)
        case IF_SVE_IJ_3A_G: // ............iiii ...gggnnnnnttttt -- SVE contiguous load (scalar plus immediate)
        case IF_SVE_IL_3A: // ............iiii ...gggnnnnnttttt -- SVE contiguous non-fault load (scalar plus immediate)
        case IF_SVE_IL_3A_A: // ............iiii ...gggnnnnnttttt -- SVE contiguous non-fault load (scalar plus
                             // immediate)
        case IF_SVE_IL_3A_B: // ............iiii ...gggnnnnnttttt -- SVE contiguous non-fault load (scalar plus
                             // immediate)
        case IF_SVE_IL_3A_C: // ............iiii ...gggnnnnnttttt -- SVE contiguous non-fault load (scalar plus
                             // immediate)
        case IF_SVE_IM_3A:   // ............iiii ...gggnnnnnttttt -- SVE contiguous non-temporal load (scalar plus
                             // immediate)
        case IF_SVE_IO_3A:   // ............iiii ...gggnnnnnttttt -- SVE load and broadcast quadword (scalar plus
                             // immediate)
        case IF_SVE_IQ_3A: // ............iiii ...gggnnnnnttttt -- SVE load multiple structures (quadwords, scalar plus
                           // immediate)
        case IF_SVE_IS_3A: // ............iiii ...gggnnnnnttttt -- SVE load multiple structures (scalar plus immediate)
        case IF_SVE_JE_3A: // ............iiii ...gggnnnnnttttt -- SVE store multiple structures (quadwords, scalar plus
                           // immediate)
        case IF_SVE_JM_3A: // ............iiii ...gggnnnnnttttt -- SVE contiguous non-temporal store (scalar plus
                           // immediate)
        case IF_SVE_JN_3C: // ............iiii ...gggnnnnnttttt -- SVE contiguous store (scalar plus immediate)
        case IF_SVE_JN_3C_D: // ............iiii ...gggnnnnnttttt -- SVE contiguous store (scalar plus immediate)
        case IF_SVE_JO_3A: // ............iiii ...gggnnnnnttttt -- SVE store multiple structures (scalar plus immediate)
            assert(insOptsScalable(id->idInsOpt()));
            assert(isVectorRegister(id->idReg1()));    // ttttt
            assert(isPredicateRegister(id->idReg2())); // ggg
            assert(isGeneralRegister(id->idReg3()));   // nnnnn
            assert(isScalableVectorSize(id->idOpSize()));

            switch (id->idIns())
            {
                case INS_sve_ld2b:
                case INS_sve_ld2h:
                case INS_sve_ld2w:
                case INS_sve_ld2d:
                case INS_sve_ld2q:
                case INS_sve_st2b:
                case INS_sve_st2h:
                case INS_sve_st2w:
                case INS_sve_st2d:
                case INS_sve_st2q:
                    assert((isValidSimm_MultipleOf<4, 2>(emitGetInsSC(id)))); // iiii
                    break;

                case INS_sve_ld3b:
                case INS_sve_ld3h:
                case INS_sve_ld3w:
                case INS_sve_ld3d:
                case INS_sve_ld3q:
                case INS_sve_st3b:
                case INS_sve_st3h:
                case INS_sve_st3w:
                case INS_sve_st3d:
                case INS_sve_st3q:
                    assert((isValidSimm_MultipleOf<4, 3>(emitGetInsSC(id)))); // iiii
                    break;

                case INS_sve_ld4b:
                case INS_sve_ld4h:
                case INS_sve_ld4w:
                case INS_sve_ld4d:
                case INS_sve_ld4q:
                case INS_sve_st4b:
                case INS_sve_st4h:
                case INS_sve_st4w:
                case INS_sve_st4d:
                case INS_sve_st4q:
                    assert((isValidSimm_MultipleOf<4, 4>(emitGetInsSC(id)))); // iiii
                    break;

                case INS_sve_ld1rqb:
                case INS_sve_ld1rqd:
                case INS_sve_ld1rqh:
                case INS_sve_ld1rqw:
                    assert((isValidSimm_MultipleOf<4, 16>(emitGetInsSC(id)))); // iiii
                    break;

                case INS_sve_ld1rob:
                case INS_sve_ld1rod:
                case INS_sve_ld1roh:
                case INS_sve_ld1row:
                    assert((isValidSimm_MultipleOf<4, 32>(emitGetInsSC(id)))); // iiii
                    break;

                default:
                    assert(isValidSimm<4>(emitGetInsSC(id))); // iiii
                    break;
            }
            break;

        case IF_SVE_JD_4A: // .........xxmmmmm ...gggnnnnnttttt -- SVE contiguous store (scalar plus scalar)
            assert(isVectorRegister(id->idReg1()));       // ttttt
            assert(isPredicateRegister(id->idReg2()));    // ggg
            assert(isGeneralRegister(id->idReg3()));      // nnnnn
            assert(isGeneralRegister(id->idReg4()));      // mmmmm
            assert(isScalableVectorSize(id->idOpSize())); // xx
            // st1h is reserved for scalable B
            assert((id->idIns() == INS_sve_st1h) ? insOptsScalableAtLeastHalf(id->idInsOpt())
                                                 : insOptsScalableStandard(id->idInsOpt()));
            break;

        case IF_SVE_JD_4B: // ..........xmmmmm ...gggnnnnnttttt -- SVE contiguous store (scalar plus scalar)
            assert(insOptsScalableWords(id->idInsOpt()));
            assert(isVectorRegister(id->idReg1()));       // ttttt
            assert(isPredicateRegister(id->idReg2()));    // ggg
            assert(isGeneralRegister(id->idReg3()));      // nnnnn
            assert(isGeneralRegister(id->idReg4()));      // mmmmm
            assert(isScalableVectorSize(id->idOpSize())); // x
            break;

        case IF_SVE_JJ_4A:   // ...........mmmmm .h.gggnnnnnttttt -- SVE 64-bit scatter store (scalar plus 64-bit scaled
                             // offsets)
        case IF_SVE_JJ_4A_B: // ...........mmmmm .h.gggnnnnnttttt -- SVE 64-bit scatter store (scalar plus 64-bit scaled
                             // offsets)
        case IF_SVE_JJ_4A_C: // ...........mmmmm .h.gggnnnnnttttt -- SVE 64-bit scatter store (scalar plus 64-bit scaled
                             // offsets)
        case IF_SVE_JJ_4A_D: // ...........mmmmm .h.gggnnnnnttttt -- SVE 64-bit scatter store (scalar plus 64-bit scaled
                             // offsets)
        case IF_SVE_JK_4A: // ...........mmmmm .h.gggnnnnnttttt -- SVE 64-bit scatter store (scalar plus 64-bit unscaled
                           // offsets)
        case IF_SVE_JK_4A_B: // ...........mmmmm .h.gggnnnnnttttt -- SVE 64-bit scatter store (scalar plus 64-bit
                             // unscaled offsets)
            assert(insOptsScalable32bitExtends(id->idInsOpt()));
            assert(isVectorRegister(id->idReg1()));    // ttttt
            assert(isPredicateRegister(id->idReg2())); // ggg
            assert(isGeneralRegister(id->idReg3()));   // nnnnn
            assert(isScalableVectorSize(id->idOpSize()));
            break;

        case IF_SVE_JN_3A: // .........xx.iiii ...gggnnnnnttttt -- SVE contiguous store (scalar plus immediate)
            imm = emitGetInsSC(id);
            assert(insOptsScalableStandard(id->idInsOpt()));
            assert(isVectorRegister(id->idReg1()));       // ttttt
            assert(isPredicateRegister(id->idReg2()));    // ggg
            assert(isGeneralRegister(id->idReg3()));      // nnnnn
            assert(isScalableVectorSize(id->idOpSize())); // xx
            assert(isValidSimm<4>(imm));                  // iiii
            break;

        case IF_SVE_JN_3B: // ..........x.iiii ...gggnnnnnttttt -- SVE contiguous store (scalar plus immediate)
            imm = emitGetInsSC(id);
            assert(insOptsScalableWords(id->idInsOpt()));
            assert(isVectorRegister(id->idReg1()));       // ttttt
            assert(isPredicateRegister(id->idReg2()));    // ggg
            assert(isGeneralRegister(id->idReg3()));      // nnnnn
            assert(isScalableVectorSize(id->idOpSize())); // x
            assert(isValidSimm<4>(imm));                  // iiii
            break;

        case IF_SVE_HW_4A:   // .........h.mmmmm ...gggnnnnnttttt -- SVE 32-bit gather load (scalar plus 32-bit unscaled
                             // offsets)
        case IF_SVE_HW_4A_A: // .........h.mmmmm ...gggnnnnnttttt -- SVE 32-bit gather load (scalar plus 32-bit unscaled
                             // offsets)
        case IF_SVE_HW_4A_B: // .........h.mmmmm ...gggnnnnnttttt -- SVE 32-bit gather load (scalar plus 32-bit unscaled
                             // offsets)
        case IF_SVE_HW_4A_C: // .........h.mmmmm ...gggnnnnnttttt -- SVE 32-bit gather load (scalar plus 32-bit unscaled
                             // offsets)
        case IF_SVE_IU_4A:   // .........h.mmmmm ...gggnnnnnttttt -- SVE 64-bit gather load (scalar plus 32-bit unpacked
                             // scaled offsets)
        case IF_SVE_IU_4A_A: // .........h.mmmmm ...gggnnnnnttttt -- SVE 64-bit gather load (scalar plus 32-bit unpacked
                             // scaled offsets)
        case IF_SVE_IU_4A_C: // .........h.mmmmm ...gggnnnnnttttt -- SVE 64-bit gather load (scalar plus 32-bit unpacked
                             // scaled offsets)
            assert(insOptsScalable32bitExtends(id->idInsOpt()));
            assert(isVectorRegister(id->idReg1()));    // ttttt
            assert(isPredicateRegister(id->idReg2())); // ggg
            assert(isGeneralRegister(id->idReg3()));   // nnnnn
            assert(isVectorRegister(id->idReg4()));    // mmmmm
            assert(isScalableVectorSize(id->idOpSize()));
            break;

        case IF_SVE_HW_4B:   // ...........mmmmm ...gggnnnnnttttt -- SVE 32-bit gather load (scalar plus 32-bit unscaled
                             // offsets)
        case IF_SVE_HW_4B_D: // ...........mmmmm ...gggnnnnnttttt -- SVE 32-bit gather load (scalar plus 32-bit unscaled
                             // offsets)
            assert(id->idInsOpt() == INS_OPTS_SCALABLE_D);
            assert(isVectorRegister(id->idReg1()));    // ttttt
            assert(isPredicateRegister(id->idReg2())); // ggg
            assert(isGeneralRegister(id->idReg3()));   // nnnnn
            assert(isVectorRegister(id->idReg4()));    // mmmmm
            assert(isScalableVectorSize(id->idOpSize()));
            break;

        case IF_SVE_IF_4A:   // ...........mmmmm ...gggnnnnnttttt -- SVE2 32-bit gather non-temporal load (vector plus
                             // scalar)
        case IF_SVE_IF_4A_A: // ...........mmmmm ...gggnnnnnttttt -- SVE2 32-bit gather non-temporal load (vector plus
                             // scalar)
            assert(insOptsScalableWords(id->idInsOpt()));
            assert(isVectorRegister(id->idReg1()));      // ttttt
            assert(isPredicateRegister(id->idReg2()));   // ggg
            assert(isVectorRegister(id->idReg3()));      // nnnnn
            assert(isGeneralRegisterOrZR(id->idReg4())); // mmmmm
            assert(isScalableVectorSize(id->idOpSize()));
            break;

        case IF_SVE_IG_4A: // ...........mmmmm ...gggnnnnnttttt -- SVE contiguous first-fault load (scalar plus scalar)
        case IF_SVE_IG_4A_D: // ...........mmmmm ...gggnnnnnttttt -- SVE contiguous first-fault load (scalar plus
                             // scalar)
        case IF_SVE_IG_4A_E: // ...........mmmmm ...gggnnnnnttttt -- SVE contiguous first-fault load (scalar plus
                             // scalar)
        case IF_SVE_IG_4A_F: // ...........mmmmm ...gggnnnnnttttt -- SVE contiguous first-fault load (scalar plus
                             // scalar)
        case IF_SVE_IG_4A_G: // ...........mmmmm ...gggnnnnnttttt -- SVE contiguous first-fault load (scalar plus
                             // scalar)
            assert(insOptsScalableStandard(id->idInsOpt()));
            assert(isVectorRegister(id->idReg1()));      // ttttt
            assert(isPredicateRegister(id->idReg2()));   // ggg
            assert(isGeneralRegister(id->idReg3()));     // nnnnn
            assert(isGeneralRegisterOrZR(id->idReg4())); // mmmmm
            assert(isScalableVectorSize(id->idOpSize()));
            break;

        case IF_SVE_II_4A: // ...........mmmmm ...gggnnnnnttttt -- SVE contiguous load (quadwords, scalar plus scalar)
            assert(id->idInsOpt() == INS_OPTS_SCALABLE_D);
            assert(isVectorRegister(id->idReg1()));    // ttttt
            assert(isPredicateRegister(id->idReg2())); // ggg
            assert(isGeneralRegister(id->idReg3()));   // nnnnn
            assert(isGeneralRegister(id->idReg4()));   // mmmmm
            assert(isScalableVectorSize(id->idOpSize()));
            break;

        case IF_SVE_II_4A_B: // ...........mmmmm ...gggnnnnnttttt -- SVE contiguous load (quadwords, scalar plus scalar)
            assert(id->idInsOpt() == INS_OPTS_SCALABLE_Q);
            assert(isVectorRegister(id->idReg1()));    // ttttt
            assert(isPredicateRegister(id->idReg2())); // ggg
            assert(isGeneralRegister(id->idReg3()));   // nnnnn
            assert(isGeneralRegister(id->idReg4()));   // mmmmm
            assert(isScalableVectorSize(id->idOpSize()));
            break;

        case IF_SVE_II_4A_H: // ...........mmmmm ...gggnnnnnttttt -- SVE contiguous load (quadwords, scalar plus scalar)
            assert(insOptsScalableWordsOrQuadwords(id->idInsOpt()));
            assert(isVectorRegister(id->idReg1()));    // ttttt
            assert(isPredicateRegister(id->idReg2())); // ggg
            assert(isGeneralRegister(id->idReg3()));   // nnnnn
            assert(isGeneralRegister(id->idReg4()));   // mmmmm
            assert(isScalableVectorSize(id->idOpSize()));
            break;

        case IF_SVE_IK_4A:   // ...........mmmmm ...gggnnnnnttttt -- SVE contiguous load (scalar plus scalar)
        case IF_SVE_IK_4A_F: // ...........mmmmm ...gggnnnnnttttt -- SVE contiguous load (scalar plus scalar)
        case IF_SVE_IK_4A_G: // ...........mmmmm ...gggnnnnnttttt -- SVE contiguous load (scalar plus scalar)
        case IF_SVE_IK_4A_H: // ...........mmmmm ...gggnnnnnttttt -- SVE contiguous load (scalar plus scalar)
        case IF_SVE_IK_4A_I: // ...........mmmmm ...gggnnnnnttttt -- SVE contiguous load (scalar plus scalar)
            assert(insOptsScalableStandard(id->idInsOpt()));
            assert(isVectorRegister(id->idReg1()));    // ttttt
            assert(isPredicateRegister(id->idReg2())); // ggg
            assert(isGeneralRegister(id->idReg3()));   // nnnnn
            assert(isGeneralRegister(id->idReg4()));   // mmmmm
            assert(isScalableVectorSize(id->idOpSize()));
            break;

        case IF_SVE_IR_4A: // ...........mmmmm ...gggnnnnnttttt -- SVE load multiple structures (quadwords, scalar plus
                           // scalar)
            assert(id->idInsOpt() == INS_OPTS_SCALABLE_Q);
            assert(isVectorRegister(id->idReg1()));    // ttttt
            assert(isPredicateRegister(id->idReg2())); // ggg
            assert(isGeneralRegister(id->idReg3()));   // nnnnn
            assert(isGeneralRegister(id->idReg4()));   // mmmmm
            assert(isScalableVectorSize(id->idOpSize()));
            break;

        case IF_SVE_IU_4B:   // ...........mmmmm ...gggnnnnnttttt -- SVE 64-bit gather load (scalar plus 32-bit unpacked
                             // scaled offsets)
        case IF_SVE_IU_4B_B: // ...........mmmmm ...gggnnnnnttttt -- SVE 64-bit gather load (scalar plus 32-bit unpacked
                             // scaled offsets)
        case IF_SVE_IU_4B_D: // ...........mmmmm ...gggnnnnnttttt -- SVE 64-bit gather load (scalar plus 32-bit unpacked
                             // scaled offsets)
            assert(id->idInsOpt() == INS_OPTS_SCALABLE_D);
            assert(isVectorRegister(id->idReg1()));    // ttttt
            assert(isPredicateRegister(id->idReg2())); // ggg
            assert(isGeneralRegister(id->idReg3()));   // nnnnn
            assert(isVectorRegister(id->idReg4()));    // mmmmm
            assert(isScalableVectorSize(id->idOpSize()));
            break;

        case IF_SVE_IW_4A: // ...........mmmmm ...gggnnnnnttttt -- SVE2 128-bit gather load (vector plus scalar)
        case IF_SVE_IY_4A: // ...........mmmmm ...gggnnnnnttttt -- SVE2 128-bit scatter store (vector plus scalar)
            assert(id->idInsOpt() == INS_OPTS_SCALABLE_Q);
            assert(isVectorRegister(id->idReg1()));      // ttttt
            assert(isPredicateRegister(id->idReg2()));   // ggg
            assert(isVectorRegister(id->idReg3()));      // nnnnn
            assert(isGeneralRegisterOrZR(id->idReg4())); // mmmmm
            assert(isScalableVectorSize(id->idOpSize()));
            break;

        case IF_SVE_IX_4A: // ...........mmmmm ...gggnnnnnttttt -- SVE2 64-bit gather non-temporal load (vector plus
                           // scalar)
            assert(id->idInsOpt() == INS_OPTS_SCALABLE_D);
            assert(isVectorRegister(id->idReg1()));      // ttttt
            assert(isPredicateRegister(id->idReg2()));   // ggg
            assert(isVectorRegister(id->idReg3()));      // nnnnn
            assert(isGeneralRegisterOrZR(id->idReg4())); // mmmmm
            assert(isScalableVectorSize(id->idOpSize()));
            break;

        case IF_SVE_IZ_4A:   // ...........mmmmm ...gggnnnnnttttt -- SVE2 32-bit scatter non-temporal store (vector plus
                             // scalar)
        case IF_SVE_IZ_4A_A: // ...........mmmmm ...gggnnnnnttttt -- SVE2 32-bit scatter non-temporal store (vector plus
                             // scalar)
        case IF_SVE_JA_4A:   // ...........mmmmm ...gggnnnnnttttt -- SVE2 64-bit scatter non-temporal store (vector plus
                             // scalar)
            assert(insOptsScalableWords(id->idInsOpt()));
            assert(isVectorRegister(id->idReg1()));      // ttttt
            assert(isPredicateRegister(id->idReg2()));   // ggg
            assert(isVectorRegister(id->idReg3()));      // nnnnn
            assert(isGeneralRegisterOrZR(id->idReg4())); // mmmmm
            assert(isScalableVectorSize(id->idOpSize()));
            break;

        case IF_SVE_JD_4C:   // ...........mmmmm ...gggnnnnnttttt -- SVE contiguous store (scalar plus scalar)
        case IF_SVE_JD_4C_A: // ...........mmmmm ...gggnnnnnttttt -- SVE contiguous store (scalar plus scalar)
            assert(insOptsScalableDoubleWordsOrQuadword(id->idInsOpt()));
            assert(isVectorRegister(id->idReg1()));    // ttttt
            assert(isPredicateRegister(id->idReg2())); // ggg
            assert(isGeneralRegister(id->idReg3()));   // nnnnn
            assert(isGeneralRegister(id->idReg4()));   // mmmmm
            assert(isScalableVectorSize(id->idOpSize()));
            break;

        case IF_SVE_JF_4A: // ...........mmmmm ...gggnnnnnttttt -- SVE store multiple structures (quadwords, scalar plus
                           // scalar)
            assert(id->idInsOpt() == INS_OPTS_SCALABLE_Q);
            assert(isVectorRegister(id->idReg1()));    // ttttt
            assert(isPredicateRegister(id->idReg2())); // ggg
            assert(isGeneralRegister(id->idReg3()));   // nnnnn
            assert(isGeneralRegister(id->idReg4()));   // mmmmm
            assert(isScalableVectorSize(id->idOpSize()));
            break;

        case IF_SVE_IN_4A: // ...........mmmmm ...gggnnnnnttttt -- SVE contiguous non-temporal load (scalar plus scalar)
        case IF_SVE_IP_4A: // ...........mmmmm ...gggnnnnnttttt -- SVE load and broadcast quadword (scalar plus scalar)
        case IF_SVE_IT_4A: // ...........mmmmm ...gggnnnnnttttt -- SVE load multiple structures (scalar plus scalar)
        case IF_SVE_JB_4A: // ...........mmmmm ...gggnnnnnttttt -- SVE contiguous non-temporal store (scalar plus
                           // scalar)
        case IF_SVE_JC_4A: // ...........mmmmm ...gggnnnnnttttt -- SVE store multiple structures (scalar plus scalar)
            assert(insOptsScalableStandard(id->idInsOpt()));
            assert(isVectorRegister(id->idReg1()));    // ttttt
            assert(isPredicateRegister(id->idReg2())); // ggg
            assert(isGeneralRegister(id->idReg3()));   // nnnnn
            assert(isGeneralRegister(id->idReg4()));   // mmmmm
            assert(isScalableVectorSize(id->idOpSize()));
            break;

        case IF_SVE_JJ_4B:   // ...........mmmmm ...gggnnnnnttttt -- SVE 64-bit scatter store (scalar plus 64-bit scaled
                             // offsets)
        case IF_SVE_JJ_4B_C: // ...........mmmmm ...gggnnnnnttttt -- SVE 64-bit scatter store (scalar plus 64-bit scaled
                             // offsets)
        case IF_SVE_JJ_4B_E: // ...........mmmmm ...gggnnnnnttttt -- SVE 64-bit scatter store (scalar plus 64-bit scaled
                             // offsets)
        case IF_SVE_JK_4B: // ...........mmmmm ...gggnnnnnttttt -- SVE 64-bit scatter store (scalar plus 64-bit unscaled
                           // offsets)
            assert(id->idInsOpt() == INS_OPTS_SCALABLE_D);
            assert(isVectorRegister(id->idReg1()));    // ttttt
            assert(isPredicateRegister(id->idReg2())); // ggg
            assert(isGeneralRegister(id->idReg3()));   // nnnnn
            assert(isVectorRegister(id->idReg4()));    // mmmmm
            assert(isScalableVectorSize(id->idOpSize()));
            break;

        case IF_SVE_GP_3A: // ........xx.....r ...gggmmmmmddddd -- SVE floating-point complex add (predicated)
            imm = emitGetInsSC(id);
            assert(insOptsScalableAtLeastHalf(id->idInsOpt()));
            assert(isVectorRegister(id->idReg1()));
            assert(isLowPredicateRegister(id->idReg2()));
            assert(isVectorRegister(id->idReg3()));
            assert(emitIsValidEncodedRotationImm90_or_270(imm));
            assert(isScalableVectorSize(id->idOpSize()));
            break;

        case IF_SVE_GT_4A: // ........xx.mmmmm .rrgggnnnnnddddd -- SVE floating-point complex multiply-add (predicated)
            imm = emitGetInsSC(id);
            assert(insOptsScalableAtLeastHalf(id->idInsOpt()));
            assert(isVectorRegister(id->idReg1()));
            assert(isLowPredicateRegister(id->idReg2()));
            assert(isVectorRegister(id->idReg3()));
            assert(isVectorRegister(id->idReg4()));
            assert(emitIsValidEncodedRotationImm0_to_270(imm));
            assert(isScalableVectorSize(id->idOpSize()));
            break;

        case IF_SVE_HI_3A: // ........xx...... ...gggnnnnn.DDDD -- SVE floating-point compare with zero
            assert(insOptsScalableAtLeastHalf(id->idInsOpt()));
            assert(isPredicateRegister(id->idReg1()));
            assert(isLowPredicateRegister(id->idReg2()));
            assert(isVectorRegister(id->idReg3()));
            assert(isScalableVectorSize(id->idOpSize()));
            break;

        case IF_SVE_HM_2A: // ........xx...... ...ggg....iddddd -- SVE floating-point arithmetic with immediate
                           // (predicated)
            imm = emitGetInsSC(id);
            assert(insOptsScalableAtLeastHalf(id->idInsOpt()));
            assert(isVectorRegister(id->idReg1()));
            assert(isLowPredicateRegister(id->idReg2()));
            assert(isScalableVectorSize(id->idOpSize()));
            assert(emitIsValidEncodedSmallFloatImm(imm));
            break;

        case IF_SVE_HN_2A: // ........xx...iii ......mmmmmddddd -- SVE floating-point trig multiply-add coefficient
            imm = emitGetInsSC(id);
            assert(insOptsScalableAtLeastHalf(id->idInsOpt()));
            assert(isVectorRegister(id->idReg1()));
            assert(isVectorRegister(id->idReg2()));
            assert(isValidUimm<3>(imm));
            assert(isScalableVectorSize(id->idOpSize()));
            break;

        case IF_SVE_HP_3A: // .............xx. ...gggnnnnnddddd -- SVE floating-point convert to integer
            assert(insOptsScalableAtLeastHalf(id->idInsOpt()));
            assert(isVectorRegister(id->idReg1()));
            assert(isLowPredicateRegister(id->idReg2()));
            assert(isVectorRegister(id->idReg3()));
            assert(isScalableVectorSize(id->idOpSize()));
            break;

        case IF_SVE_HU_4B: // ...........mmmmm ...gggnnnnnddddd -- SVE floating-point multiply-accumulate writing addend
            assert(id->idInsOpt() == INS_OPTS_SCALABLE_H);
            assert(isVectorRegister(id->idReg1()));
            assert(isLowPredicateRegister(id->idReg2()));
            assert(isVectorRegister(id->idReg3()));
            assert(isVectorRegister(id->idReg4()));
            assert(isScalableVectorSize(id->idOpSize()));
            break;

        case IF_SVE_HV_4A: // ........xx.aaaaa ...gggmmmmmddddd -- SVE floating-point multiply-accumulate writing
                           // multiplicand
            assert(insOptsScalableAtLeastHalf(id->idInsOpt()));
            assert(isVectorRegister(id->idReg1()));
            assert(isLowPredicateRegister(id->idReg2()));
            assert(isVectorRegister(id->idReg3()));
            assert(isVectorRegister(id->idReg4()));
            assert(isScalableVectorSize(id->idOpSize()));
            break;

        case IF_SVE_ID_2A: // ..........iiiiii ...iiinnnnn.TTTT -- SVE load predicate register
        case IF_SVE_JG_2A: // ..........iiiiii ...iiinnnnn.TTTT -- SVE store predicate register
            assert(insOptsNone(id->idInsOpt()));
            assert(isScalableVectorSize(id->idOpSize()));
            assert(isPredicateRegister(id->idReg1()));   // TTTT
            assert(isGeneralRegisterOrZR(id->idReg2())); // nnnnn
            assert(isValidSimm<9>(emitGetInsSC(id)));    // iii
            break;

        case IF_SVE_IE_2A: // ..........iiiiii ...iiinnnnnttttt -- SVE load vector register
        case IF_SVE_JH_2A: // ..........iiiiii ...iiinnnnnttttt -- SVE store vector register
            assert(insOptsNone(id->idInsOpt()));
            assert(isScalableVectorSize(id->idOpSize()));
            assert(isVectorRegister(id->idReg1()));      // ttttt
            assert(isGeneralRegisterOrZR(id->idReg2())); // nnnnn
            assert(isValidSimm<9>(emitGetInsSC(id)));    // iii
            break;

        case IF_SVE_GG_3A: // ........ii.mmmmm ......nnnnnddddd -- SVE2 lookup table with 2-bit indices and 16-bit
                           // element size
            assert(isVectorRegister(id->idReg1()));   // ddddd
            assert(isVectorRegister(id->idReg2()));   // nnnnn
            assert(isVectorRegister(id->idReg3()));   // mmmmm
            assert(isValidUimm<2>(emitGetInsSC(id))); // ii
            assert(id->idInsOpt() == INS_OPTS_SCALABLE_B);
            break;

        case IF_SVE_GH_3B:   // ........ii.mmmmm ......nnnnnddddd -- SVE2 lookup table with 4-bit indices and 16-bit
                             // element size
        case IF_SVE_GH_3B_B: // ........ii.mmmmm ......nnnnnddddd -- SVE2 lookup table with 4-bit indices and 16-bit
                             // element size
            assert(isVectorRegister(id->idReg1()));   // ddddd
            assert(isVectorRegister(id->idReg2()));   // nnnnn
            assert(isVectorRegister(id->idReg3()));   // mmmmm
            assert(isValidUimm<2>(emitGetInsSC(id))); // ii
            assert(id->idInsOpt() == INS_OPTS_SCALABLE_H);
            break;

        case IF_SVE_GG_3B: // ........ii.mmmmm ...i..nnnnnddddd -- SVE2 lookup table with 2-bit indices and 16-bit
                           // element size
            assert(isVectorRegister(id->idReg1()));   // ddddd
            assert(isVectorRegister(id->idReg2()));   // nnnnn
            assert(isVectorRegister(id->idReg3()));   // mmmmm
            assert(isValidUimm<3>(emitGetInsSC(id))); // ii
                                                      // i
            assert(id->idInsOpt() == INS_OPTS_SCALABLE_H);
            break;

        case IF_SVE_GH_3A: // ........i..mmmmm ......nnnnnddddd -- SVE2 lookup table with 4-bit indices and 16-bit
                           // element size
            assert(insOptsScalable(id->idInsOpt()));
            assert(isVectorRegister(id->idReg1()));   // ddddd
            assert(isVectorRegister(id->idReg2()));   // nnnnn
            assert(isVectorRegister(id->idReg3()));   // mmmmm
            assert(isValidUimm<1>(emitGetInsSC(id))); // i
            assert(id->idInsOpt() == INS_OPTS_SCALABLE_B);
            break;

        case IF_SVE_HY_3A: // .........h.mmmmm ...gggnnnnn.oooo -- SVE 32-bit gather prefetch (scalar plus 32-bit scaled
                           // offsets)
        case IF_SVE_HY_3A_A: // .........h.mmmmm ...gggnnnnn.oooo -- SVE 32-bit gather prefetch (scalar plus 32-bit
                             // scaled offsets)
            assert(insOptsScalable32bitExtends(id->idInsOpt()));
            assert(isLowPredicateRegister(id->idReg1()));
            assert(isGeneralRegister(id->idReg2()));
            assert(isVectorRegister(id->idReg3()));
            assert(isScalableVectorSize(id->idOpSize()));
            break;

        case IF_SVE_HY_3B: // ...........mmmmm ...gggnnnnn.oooo -- SVE 32-bit gather prefetch (scalar plus 32-bit scaled
                           // offsets)
            assert(id->idInsOpt() == INS_OPTS_SCALABLE_D);
            assert(isLowPredicateRegister(id->idReg1()));
            assert(isGeneralRegister(id->idReg2()));
            assert(isVectorRegister(id->idReg3()));
            assert(isScalableVectorSize(id->idOpSize()));
            break;

        case IF_SVE_IB_3A: // ...........mmmmm ...gggnnnnn.oooo -- SVE contiguous prefetch (scalar plus scalar)
            assert(insOptsNone(id->idInsOpt()));
            assert(isLowPredicateRegister(id->idReg1()));
            assert(isGeneralRegister(id->idReg2()));
            assert(isGeneralRegister(id->idReg3()));
            assert(isScalableVectorSize(id->idOpSize()));
            break;

        case IF_SVE_HZ_2A_B: // ...........iiiii ...gggnnnnn.oooo -- SVE 32-bit gather prefetch (vector plus immediate)
            assert(insOptsScalableWords(id->idInsOpt()));
            assert(isLowPredicateRegister(id->idReg1()));
            assert(isVectorRegister(id->idReg2()));
            assert(isScalableVectorSize(id->idOpSize()));
            break;

        case IF_SVE_IA_2A: // ..........iiiiii ...gggnnnnn.oooo -- SVE contiguous prefetch (scalar plus immediate)
            assert(insOptsNone(id->idInsOpt()));
            assert(isLowPredicateRegister(id->idReg1()));
            assert(isGeneralRegister(id->idReg2()));
            assert(isScalableVectorSize(id->idOpSize()));
            break;

        case IF_SVE_HX_3A_B: // ...........iiiii ...gggnnnnnttttt -- SVE 32-bit gather load (vector plus immediate)
            assert(insOptsScalableWords(id->idInsOpt()));
            assert(isScalableVectorSize(id->idOpSize()));
            assert(isVectorRegister(id->idReg1()));
            assert(isLowPredicateRegister(id->idReg2()));
            assert(isVectorRegister(id->idReg3()));
            assert(isValidUimm<5>(emitGetInsSC(id)));
            break;

        case IF_SVE_HX_3A_E: // ...........iiiii ...gggnnnnnttttt -- SVE 32-bit gather load (vector plus immediate)
            assert(insOptsScalableWords(id->idInsOpt()));
            assert(isScalableVectorSize(id->idOpSize()));
            assert(isVectorRegister(id->idReg1()));
            assert(isLowPredicateRegister(id->idReg2()));
            assert(isVectorRegister(id->idReg3()));
            break;

        case IF_SVE_IV_3A: // ...........iiiii ...gggnnnnnttttt -- SVE 64-bit gather load (vector plus immediate)
            assert(id->idInsOpt() == INS_OPTS_SCALABLE_D);
            assert(isScalableVectorSize(id->idOpSize()));
            assert(isVectorRegister(id->idReg1()));
            assert(isLowPredicateRegister(id->idReg2()));
            assert(isVectorRegister(id->idReg3()));
            break;

        case IF_SVE_JI_3A_A: // ...........iiiii ...gggnnnnnttttt -- SVE 32-bit scatter store (vector plus immediate)
            assert(insOptsScalableWords(id->idInsOpt()));
            assert(isScalableVectorSize(id->idOpSize()));
            assert(isVectorRegister(id->idReg1()));
            assert(isLowPredicateRegister(id->idReg2()));
            assert(isVectorRegister(id->idReg3()));
            break;

        case IF_SVE_JL_3A: // ...........iiiii ...gggnnnnnttttt -- SVE 64-bit scatter store (vector plus immediate)
            assert(id->idInsOpt() == INS_OPTS_SCALABLE_D);
            assert(isScalableVectorSize(id->idOpSize()));
            assert(isVectorRegister(id->idReg1()));
            assert(isLowPredicateRegister(id->idReg2()));
            assert(isVectorRegister(id->idReg3()));
            assert((isValidUimm_MultipleOf<5, 8>(emitGetInsSC(id))));
            break;

        case IF_SVE_IC_3A: // ..........iiiiii ...gggnnnnnttttt -- SVE load and broadcast element
            assert(id->idInsOpt() == INS_OPTS_SCALABLE_D);
            assert(isScalableVectorSize(id->idOpSize()));
            assert(isVectorRegister(id->idReg1()));
            assert(isLowPredicateRegister(id->idReg2()));
            assert(isGeneralRegister(id->idReg3()));
            break;

        case IF_SVE_IC_3A_A: // ..........iiiiii ...gggnnnnnttttt -- SVE load and broadcast element
            assert(insOptsScalableWords(id->idInsOpt()));
            assert(isScalableVectorSize(id->idOpSize()));
            assert(isVectorRegister(id->idReg1()));
            assert(isLowPredicateRegister(id->idReg2()));
            assert(isGeneralRegister(id->idReg3()));
            break;

        case IF_SVE_IC_3A_B: // ..........iiiiii ...gggnnnnnttttt -- SVE load and broadcast element
            assert(insOptsScalableAtLeastHalf(id->idInsOpt()));
            assert(isScalableVectorSize(id->idOpSize()));
            assert(isVectorRegister(id->idReg1()));
            assert(isLowPredicateRegister(id->idReg2()));
            assert(isGeneralRegister(id->idReg3()));
            break;

        case IF_SVE_IC_3A_C: // ..........iiiiii ...gggnnnnnttttt -- SVE load and broadcast element
            assert(insOptsScalableStandard(id->idInsOpt()));
            assert(isScalableVectorSize(id->idOpSize()));
            assert(isVectorRegister(id->idReg1()));
            assert(isLowPredicateRegister(id->idReg2()));
            assert(isGeneralRegister(id->idReg3()));
            break;

        case IF_SVE_BI_2A: // ................ ......nnnnnddddd -- SVE constructive prefix (unpredicated)
            assert(insOptsNone(id->idInsOpt()));
            assert(isVectorRegister(id->idReg1()));
            assert(isVectorRegister(id->idReg2()));
            break;

        case IF_SVE_HH_2A: // ................ ......nnnnnddddd -- SVE2 FP8 upconverts
            assert(id->idInsOpt() == INS_OPTS_SCALABLE_H);
            assert(isVectorRegister(id->idReg1()));
            assert(isVectorRegister(id->idReg2()));
            break;

        case IF_SVE_CB_2A: // ........xx...... ......nnnnnddddd -- SVE broadcast general register
            assert(insOptsScalableStandard(id->idInsOpt()));
            assert(isVectorRegister(id->idReg1()));
            assert(isGeneralRegisterOrZR(id->idReg2())); // ZR is SP
            break;

        case IF_SVE_CG_2A: // ........xx...... ......nnnnnddddd -- SVE reverse vector elements
            assert(insOptsScalableStandard(id->idInsOpt()));
            assert(isVectorRegister(id->idReg1()));
            assert(isVectorRegister(id->idReg2()));
            break;

        case IF_SVE_BJ_2A: // ........xx...... ......nnnnnddddd -- SVE floating-point exponential accelerator
        case IF_SVE_HF_2A: // ........xx...... ......nnnnnddddd -- SVE floating-point reciprocal estimate (unpredicated)
            assert(insOptsScalableAtLeastHalf(id->idInsOpt()));
            assert(isVectorRegister(id->idReg1()));
            assert(isVectorRegister(id->idReg2()));
            break;

        case IF_SVE_CH_2A: // ........xx...... ......nnnnnddddd -- SVE unpack vector elements
            assert(insOptsScalableWide(id->idInsOpt()));
            assert(isVectorRegister(id->idReg1()));
            assert(isVectorRegister(id->idReg2()));
            break;

        case IF_SVE_BF_2A: // ........xx.xxiii ......nnnnnddddd -- SVE bitwise shift by immediate (unpredicated)
        case IF_SVE_FT_2A: // ........xx.xxiii ......nnnnnddddd -- SVE2 bitwise shift and insert
        case IF_SVE_FU_2A: // ........xx.xxiii ......nnnnnddddd -- SVE2 bitwise shift right and accumulate
            imm = emitGetInsSC(id);
            assert(isValidVectorShiftAmount(imm, optGetSveElemsize(id->idInsOpt()),
                                            emitInsIsVectorRightShift(id->idIns())));
            assert(insOptsScalableStandard(id->idInsOpt()));
            assert(isVectorRegister(id->idReg1()));
            assert(isVectorRegister(id->idReg2()));
            assert(isScalableVectorSize(id->idOpSize()));
            break;

        case IF_SVE_BW_2A: // ........ii.xxxxx ......nnnnnddddd -- SVE broadcast indexed element
            imm = emitGetInsSC(id);
            assert(insOptsScalable(id->idInsOpt()));
            assert(isVectorRegister(id->idReg1()));
            assert(isVectorRegister(id->idReg2()));
            assert(isValidBroadcastImm(imm, optGetSveElemsize(id->idInsOpt())));
            break;

        case IF_SVE_BX_2A: // ...........ixxxx ......nnnnnddddd -- sve_int_perm_dupq_i
            imm = emitGetInsSC(id);
            assert(insOptsScalableStandard(id->idInsOpt()));
            assert(isVectorRegister(id->idReg1()));
            assert(isVectorRegister(id->idReg2()));
            assert(isScalableVectorSize(id->idOpSize()));
            switch (id->idInsOpt())
            {
                case INS_OPTS_SCALABLE_B:
                    assert(isValidUimm<4>(imm));
                    break;

                case INS_OPTS_SCALABLE_H:
                    assert(isValidUimm<3>(imm));
                    break;

                case INS_OPTS_SCALABLE_S:
                    assert(isValidUimm<2>(imm));
                    break;

                case INS_OPTS_SCALABLE_D:
                    assert(isValidUimm<1>(imm));
                    break;

                default:
                    break;
            }
            break;

        case IF_SVE_BY_2A: // ............iiii ......mmmmmddddd -- sve_int_perm_extq
            imm = emitGetInsSC(id);
            assert(id->idInsOpt() == INS_OPTS_SCALABLE_B);
            assert(isVectorRegister(id->idReg1()));
            assert(isVectorRegister(id->idReg2()));
            assert(isScalableVectorSize(id->idOpSize()));
            assert(isValidUimm<4>(imm));
            break;

        default:
            printf("unexpected format %s\n", emitIfName(id->idInsFmt()));
            assert(!"Unexpected format");
            break;
    }
}
#endif // DEBUG

//--------------------------------------------------------------------
// emitDispInsSveHelp: Dump the given SVE instruction to jitstdout.
//
// Arguments:
//   id - The instruction
//
void emitter::emitDispInsSveHelp(instrDesc* id)
{
    instruction ins  = id->idIns();
    insFormat   fmt  = id->idInsFmt();
    emitAttr    size = id->idOpSize();

    switch (fmt)
    {
        ssize_t    imm;
        bitMaskImm bmi;

        //  <Zdn>.<T>, <Pg>/M, <Zdn>.<T>, <Zm>.<T>
        case IF_SVE_AA_3A: // ........xx...... ...gggmmmmmddddd
        case IF_SVE_AC_3A: // ........xx...... ...gggmmmmmddddd -- SVE integer divide vectors (predicated)
        case IF_SVE_GR_3A: // ........xx...... ...gggmmmmmddddd -- SVE2 floating-point pairwise operations
        case IF_SVE_HL_3A: // ........xx...... ...gggmmmmmddddd -- SVE floating-point arithmetic (predicated)
        // <Zdn>.D, <Pg>/M, <Zdn>.D, <Zm>.D
        case IF_SVE_AB_3B: // ................ ...gggmmmmmddddd -- SVE integer add/subtract vectors (predicated)
        // <Zdn>.H, <Pg>/M, <Zdn>.H, <Zm>.H
        case IF_SVE_HL_3B: // ................ ...gggmmmmmddddd -- SVE floating-point arithmetic (predicated)
            emitDispSveReg(id->idReg1(), id->idInsOpt(), true);                                    // ddddd
            emitDispLowPredicateReg(id->idReg2(), insGetPredicateType(fmt), id->idInsOpt(), true); // ggg
            emitDispSveReg(id->idReg1(), id->idInsOpt(), true);                                    // ddddd
            emitDispSveReg(id->idReg3(), id->idInsOpt(), false);                                   // mmmmm
            break;

        // <Zd>.<T>, <Pg>/<ZM>, <Zn>.<T>
        case IF_SVE_AH_3A: // ........xx.....M ...gggnnnnnddddd -- SVE constructive prefix (predicated)
        {
            PredicateType ptype = (id->idPredicateReg2Merge()) ? PREDICATE_MERGE : PREDICATE_ZERO;
            emitDispSveReg(id->idReg1(), id->idInsOpt(), true);                 // nnnnn
            emitDispLowPredicateReg(id->idReg2(), ptype, id->idInsOpt(), true); // ggg
            emitDispSveReg(id->idReg3(), id->idInsOpt(), false);                // ddddd
            break;
        }

        // <Zdn>.<T>, <Pg>/M, <Zdn>.<T>, #<const>
        case IF_SVE_AM_2A: // ........xx...... ...gggxxiiiddddd -- SVE bitwise shift by immediate (predicated)
            emitDispSveReg(id->idReg1(), id->idInsOpt(), true);                                    // ddddd
            emitDispLowPredicateReg(id->idReg2(), insGetPredicateType(fmt), id->idInsOpt(), true); // ggg
            emitDispSveReg(id->idReg1(), id->idInsOpt(), true);                                    // ddddd
            emitDispImm(emitGetInsSC(id), false);                                                  // iiii
            break;

        // <Zdn>.<T>, <Pg>/M, <Zdn>.<T>, <Zm>.D
        case IF_SVE_AO_3A: // ........xx...... ...gggmmmmmddddd -- SVE bitwise shift by wide elements (predicated)
            emitDispSveReg(id->idReg1(), id->idInsOpt(), true);                                    // ddddd
            emitDispLowPredicateReg(id->idReg2(), insGetPredicateType(fmt), id->idInsOpt(), true); // ggg
            emitDispSveReg(id->idReg1(), id->idInsOpt(), true);                                    // ddddd
            emitDispSveReg(id->idReg3(), INS_OPTS_SCALABLE_D, false);                              // mmmmm
            break;

        // <Zda>.<T>, <Pg>/M, <Zn>.<T>, <Zm>.<T>
        // <Zdn>.<T>, <Pg>/M, <Zm>.<T>, <Za>.<T>
        case IF_SVE_AR_4A: // ........xx.mmmmm ...gggnnnnnddddd -- SVE integer multiply-accumulate writing addend
                           // (predicated)
        case IF_SVE_AS_4A: // ........xx.mmmmm ...gggaaaaaddddd -- SVE integer multiply-add writing multiplicand
                           // (predicated)
        case IF_SVE_HU_4A: // ........xx.mmmmm ...gggnnnnnddddd -- SVE floating-point multiply-accumulate writing addend
        // <Zd>.<T>, <Pg>/Z, <Zn>.<T>, <Zm>.<T>
        case IF_SVE_GI_4A: // ........xx.mmmmm ...gggnnnnnddddd -- SVE2 histogram generation (vector)
            emitDispSveReg(id->idReg1(), id->idInsOpt(), true);                                    // ddddd
            emitDispLowPredicateReg(id->idReg2(), insGetPredicateType(fmt), id->idInsOpt(), true); // ggg
            emitDispSveReg(id->idReg3(), id->idInsOpt(), true);
            emitDispSveReg(id->idReg4(), id->idInsOpt(), false);
            break;

        // <Zd>.<T>, <Zn>.<T>, <Zm>.<T>
        case IF_SVE_AT_3A: // ........xx.mmmmm ......nnnnnddddd
        // <Zda>.<T>, <Zn>.<T>, <Zm>.<T>
        case IF_SVE_EM_3A: // ........xx.mmmmm ......nnnnnddddd -- SVE2 saturating multiply-add high
        case IF_SVE_FW_3A: // ........xx.mmmmm ......nnnnnddddd -- SVE2 integer absolute difference and accumulate
        // <Zd>.Q, <Zn>.Q, <Zm>.Q
        case IF_SVE_BR_3B: // ...........mmmmm ......nnnnnddddd -- SVE permute vector segments
        // <Zda>.D, <Zn>.D, <Zm>.D
        case IF_SVE_HD_3A_A: // ...........mmmmm ......nnnnnddddd -- SVE floating point matrix multiply accumulate
        // <Zd>.D, <Zn>.D, <Zm>.D
        case IF_SVE_AT_3B: // ...........mmmmm ......nnnnnddddd -- SVE integer add/subtract vectors (unpredicated)
        // <Zd>.B, <Zn>.B, <Zm>.B
        case IF_SVE_GF_3A: // ........xx.mmmmm ......nnnnnddddd -- SVE2 histogram generation (segment)
        case IF_SVE_BD_3B: // ...........mmmmm ......nnnnnddddd -- SVE2 integer multiply vectors (unpredicated)
        // <Zd>.D, <Zn>.D, <Zm>.D
        // <Zd>.S, <Zn>.S, <Zm>.S
        case IF_SVE_GJ_3A: // ...........mmmmm ......nnnnnddddd -- SVE2 crypto constructive binary operations
        // <Zd>.H, <Zn>.H, <Zm>.H
        case IF_SVE_GW_3B: // ...........mmmmm ......nnnnnddddd -- SVE FP clamp
        case IF_SVE_HK_3B: // ...........mmmmm ......nnnnnddddd -- SVE floating-point arithmetic (unpredicated)
            emitDispSveReg(id->idReg1(), id->idInsOpt(), true);  // ddddd
            emitDispSveReg(id->idReg2(), id->idInsOpt(), true);  // nnnnn/mmmmm
            emitDispSveReg(id->idReg3(), id->idInsOpt(), false); // mmmmm/aaaaa
            break;

        // <Zd>.D, <Zn>.D, <Zm>.D
        case IF_SVE_AU_3A: // ...........mmmmm ......nnnnnddddd -- SVE bitwise logical operations (unpredicated)
            emitDispSveReg(id->idReg1(), id->idInsOpt(), true); // ddddd
            if (id->idIns() == INS_sve_mov)
            {
                emitDispSveReg(id->idReg2(), id->idInsOpt(), false); // nnnnn/mmmmm
            }
            else
            {
                emitDispSveReg(id->idReg2(), id->idInsOpt(), true);  // nnnnn/mmmmm
                emitDispSveReg(id->idReg3(), id->idInsOpt(), false); // mmmmm/aaaaa
            }
            break;

        // <Zda>.D, <Zn>.D, <Zm>.D
        case IF_SVE_EW_3A: // ...........mmmmm ......nnnnnddddd -- SVE2 multiply-add (checked pointer)
        // <Zdn>.D, <Zm>.D, <Za>.D
        case IF_SVE_EW_3B: // ...........mmmmm ......aaaaaddddd -- SVE2 multiply-add (checked pointer)
            emitDispSveReg(id->idReg1(), INS_OPTS_SCALABLE_D, true);  // ddddd
            emitDispSveReg(id->idReg2(), INS_OPTS_SCALABLE_D, true);  // nnnnn
            emitDispSveReg(id->idReg3(), INS_OPTS_SCALABLE_D, false); // mmmmm
            break;

        // <Zdn>.D, <Zdn>.D, <Zm>.D, <Zk>.D
        case IF_SVE_AV_3A: // ...........mmmmm ......kkkkkddddd -- SVE2 bitwise ternary operations
            emitDispSveReg(id->idReg1(), id->idInsOpt(), true);  // ddddd
            emitDispSveReg(id->idReg1(), id->idInsOpt(), true);  // ddddd
            emitDispSveReg(id->idReg2(), id->idInsOpt(), true);  // mmmmm
            emitDispSveReg(id->idReg3(), id->idInsOpt(), false); // kkkkk
            break;

        // <Zd>.<T>, #<imm1>, #<imm2>
        case IF_SVE_AX_1A: // ........xx.iiiii ......iiiiiddddd -- SVE index generation (immediate start, immediate
                           // increment)
        {
            ssize_t imm1;
            ssize_t imm2;
            insSveDecodeTwoSimm5(emitGetInsSC(id), &imm1, &imm2);
            emitDispSveReg(id->idReg1(), id->idInsOpt(), true); // ddddd
            emitDispImm(imm1, true);                            // iiiii
            emitDispImm(imm2, false);                           // iiiii
            break;
        }

        // <Zd>.<T>, #<imm>, <R><m>
        case IF_SVE_AY_2A: // ........xx.mmmmm ......iiiiiddddd -- SVE index generation (immediate start, register
                           // increment)
        {
            const emitAttr intRegSize = (id->idInsOpt() == INS_OPTS_SCALABLE_D) ? EA_8BYTE : EA_4BYTE;
            emitDispSveReg(id->idReg1(), id->idInsOpt(), true); // ddddd
            emitDispImm(emitGetInsSC(id), true);                // iiiii
            emitDispReg(id->idReg2(), intRegSize, false);       // mmmmm
            break;
        }

        // <Zd>.<T>, <R><n>, #<imm>
        case IF_SVE_AZ_2A: // ........xx.iiiii ......nnnnnddddd -- SVE index generation (register start, immediate
                           // increment)
        {
            const emitAttr intRegSize = (id->idInsOpt() == INS_OPTS_SCALABLE_D) ? EA_8BYTE : EA_4BYTE;
            emitDispSveReg(id->idReg1(), id->idInsOpt(), true); // ddddd
            emitDispReg(id->idReg2(), intRegSize, true);        // mmmmm
            emitDispImm(emitGetInsSC(id), false);               // iiiii
            break;
        }

        // <Zda>.H, <Zn>.B, <Zm>.B
        case IF_SVE_GN_3A:   // ...........mmmmm ......nnnnnddddd -- SVE2 FP8 multiply-add long
        case IF_SVE_HA_3A_E: // ...........mmmmm ......nnnnnddddd -- SVE BFloat16 floating-point dot product
            emitDispSveReg(id->idReg1(), INS_OPTS_SCALABLE_H, true); // ddddd
            emitDispSveReg(id->idReg2(), id->idInsOpt(), true);      // nnnnn
            emitDispSveReg(id->idReg3(), id->idInsOpt(), false);     // mmmmm
            break;

        // <Zd>.<T>, <Zn>.<T>, <Zm>.<T>
        // <Zd>.<T>, {<Zn>.<T>}, <Zm>.<T>
        case IF_SVE_BZ_3A: // ........xx.mmmmm ......nnnnnddddd -- SVE table lookup (three sources)
            emitDispSveReg(id->idReg1(), id->idInsOpt(), true); // ddddd
            if (id->idIns() == INS_sve_tbl)
            {
                emitDispSveConsecutiveRegList(id->idReg2(), 1, id->idInsOpt(), true); // nnnnn
            }
            else
            {
                assert(id->idIns() == INS_sve_tbx);
                emitDispSveReg(id->idReg2(), id->idInsOpt(), true); // nnnnn
            }
            emitDispSveReg(id->idReg3(), id->idInsOpt(), false); // mmmmm
            break;

        // <Zd>.<T>, <Zn>.<T>, <Zm>.<T>
        // <Zd>.<T>, {<Zn>.<T>}, <Zm>.<T>
        case IF_SVE_EX_3A: // ........xx.mmmmm ......nnnnnddddd -- SVE permute vector elements (quadwords)
            emitDispSveReg(id->idReg1(), id->idInsOpt(), true); // ddddd
            if (id->idIns() == INS_sve_tblq)
            {
                emitDispSveConsecutiveRegList(id->idReg2(), 1, id->idInsOpt(), true); // nnnnn
            }
            else
            {
                emitDispSveReg(id->idReg2(), id->idInsOpt(), true); // nnnnn
            }
            emitDispSveReg(id->idReg3(), id->idInsOpt(), false); // mmmmm
            break;

        // <Zd>.<T>, {<Zn1>.<T>, <Zn2>.<T>}, <Zm>.<T>
        case IF_SVE_BZ_3A_A: // ........xx.mmmmm ......nnnnnddddd -- SVE table lookup (three sources)
            emitDispSveReg(id->idReg1(), id->idInsOpt(), true);                   // ddddd
            emitDispSveConsecutiveRegList(id->idReg2(), 2, id->idInsOpt(), true); // nnnnn
            emitDispSveReg(id->idReg3(), id->idInsOpt(), false);                  // mmmmm
            break;

        // <Zd>.<T>, <R><n>, <R><m>
        case IF_SVE_BA_3A: // ........xx.mmmmm ......nnnnnddddd -- SVE index generation (register start, register
                           // increment)
            emitDispSveReg(id->idReg1(), id->idInsOpt(), true); // ddddd
            emitDispReg(id->idReg2(), size, true);              // nnnnn
            emitDispReg(id->idReg3(), size, false);             // mmmmm
            break;

        // <Xd>{, <pattern>{, MUL #<imm>}}
        case IF_SVE_BL_1A: // ............iiii ......pppppddddd -- SVE element count
        // <Xdn>{, <pattern>{, MUL #<imm>}}
        case IF_SVE_BM_1A: // ............iiii ......pppppddddd -- SVE inc/dec register by element count
            imm = emitGetInsSC(id);
            emitDispReg(id->idReg1(), size, true);             // ddddd
            emitDispSvePattern(id->idSvePattern(), (imm > 1)); // ppppp
            if (imm > 1)
            {
                printf("mul ");
                emitDispImm(imm, false, false); // iiii
            }
            break;

        // <Zdn>.D{, <pattern>{, MUL #<imm>}}
        // <Zdn>.H{, <pattern>{, MUL #<imm>}}
        // <Zdn>.S{, <pattern>{, MUL #<imm>}}
        case IF_SVE_BN_1A: // ............iiii ......pppppddddd -- SVE inc/dec vector by element count
        case IF_SVE_BP_1A: // ............iiii ......pppppddddd -- SVE saturating inc/dec vector by element count
            imm = emitGetInsSC(id);
            emitDispSveReg(id->idReg1(), id->idInsOpt(), true); // ddddd
            emitDispSvePattern(id->idSvePattern(), (imm > 1));  // ppppp
            if (imm > 1)
            {
                printf("mul ");
                emitDispImm(imm, false, false); // iiii
            }
            break;

        // <Zdn>.<T>, <Zdn>.<T>, #<const>
        case IF_SVE_BS_1A: // ..............ii iiiiiiiiiiiddddd -- SVE bitwise logical with immediate (unpredicated)
            emitDispSveReg(id->idReg1(), id->idInsOpt(), true); // ddddd

            FALLTHROUGH;
        // <Zd>.<T>, #<const>
        case IF_SVE_BT_1A: // ..............ii iiiiiiiiiiiddddd -- SVE broadcast bitmask immediate
            emitDispSveReg(id->idReg1(), id->idInsOpt(), true); // ddddd
            bmi.immNRS = (unsigned)emitGetInsSC(id);
            imm        = emitDecodeBitMaskImm(bmi, optGetSveElemsize(id->idInsOpt()));
            emitDispImm(imm, false); // iiiiiiiiiiiii
            break;

        // <Xdn>, <Wdn>{, <pattern>{, MUL #<imm>}}
        // <Xdn>{, <pattern>{, MUL #<imm>}}
        // <Wdn>{, <pattern>{, MUL #<imm>}}
        case IF_SVE_BO_1A: // ...........Xiiii ......pppppddddd -- SVE saturating inc/dec register by element count
            switch (id->idIns())
            {
                case INS_sve_sqincb:
                case INS_sve_sqdecb:
                case INS_sve_sqinch:
                case INS_sve_sqdech:
                case INS_sve_sqincw:
                case INS_sve_sqdecw:
                case INS_sve_sqincd:
                case INS_sve_sqdecd:
                    emitDispReg(id->idReg1(), EA_8BYTE, true); // ddddd

                    if (size == EA_4BYTE)
                    {
                        emitDispReg(id->idReg1(), EA_4BYTE, true);
                    }
                    break;

                default:
                    emitDispReg(id->idReg1(), size, true); // ddddd
                    break;
            }

            imm = emitGetInsSC(id);
            emitDispSvePattern(id->idSvePattern(), (imm > 1)); // ppppp
            if (imm > 1)
            {
                printf("mul ");
                emitDispImm(imm, false, false); // iiii
            }
            break;

        // <Zd>.B, {<Zn1>.B, <Zn2>.B }, #<imm>
        case IF_SVE_BQ_2A: // ...........iiiii ...iiinnnnnddddd -- SVE extract vector (immediate offset, destructive)
            imm = emitGetInsSC(id);
            emitDispSveReg(id->idReg1(), id->idInsOpt(), true);           // ddddd
            emitDispVectorRegList(id->idReg2(), 2, id->idInsOpt(), true); // nnnnn
            emitDispImm(imm, false);                                      // iiiii iii
            break;

        // <Zdn>.B, <Zdn>.B, <Zm>.B, #<imm>
        case IF_SVE_BQ_2B: // ...........iiiii ...iiimmmmmddddd -- SVE extract vector (immediate offset, destructive)
            imm = emitGetInsSC(id);
            emitDispSveReg(id->idReg1(), id->idInsOpt(), true); // ddddd
            emitDispSveReg(id->idReg1(), id->idInsOpt(), true);
            emitDispSveReg(id->idReg2(), id->idInsOpt(), true); // mmmmm
            emitDispImm(imm, false);                            // iiiii iii
            break;

        // <Zd>.<T>, <Pg>/M, #<const>
        case IF_SVE_BU_2A: // ........xx..gggg ...iiiiiiiiddddd -- SVE copy floating-point immediate (predicated)
            emitDispSveReg(id->idReg1(), id->idInsOpt(), true);                                           // ddddd
            emitDispPredicateReg(id->idReg2(), insGetPredicateType(id->idInsFmt()), INS_OPTS_NONE, true); // gggg
            emitDispFloatImm(emitGetInsSC(id));                                                           // iiiiiiii
            break;

        // <Zd>.<T>, <Zn>.<T>, <Zm>.D
        case IF_SVE_BG_3A: // ........xx.mmmmm ......nnnnnddddd -- SVE bitwise shift by wide elements (unpredicated)
            emitDispSveReg(id->idReg1(), id->idInsOpt(), true);       // ddddd
            emitDispSveReg(id->idReg2(), id->idInsOpt(), true);       // nnnnn
            emitDispSveReg(id->idReg3(), INS_OPTS_SCALABLE_D, false); // mmmmm
            break;

        // <Zd>.<T>, [<Zn>.<T>, <Zm>.<T>{, <mod> <amount>}]
        case IF_SVE_BH_3A: // .........x.mmmmm ....hhnnnnnddddd -- SVE address generation
            emitDispSveReg(id->idReg1(), id->idInsOpt(), true); // ddddd
            printf("[");
            emitDispSveReg(id->idReg2(), id->idInsOpt(), true);
            emitDispSveReg(id->idReg3(), id->idInsOpt(), emitGetInsSC(id) > 0);
            emitDispSveExtendOptsModN(INS_OPTS_LSL, emitGetInsSC(id));
            printf("]");
            break;

        // <Zd>.D, [<Zn>.D, <Zm>.D, SXTW{ <amount>}]
        case IF_SVE_BH_3B: // ...........mmmmm ....hhnnnnnddddd -- SVE address generation
            emitDispSveReg(id->idReg1(), id->idInsOpt(), true); // ddddd
            printf("[");
            emitDispSveReg(id->idReg2(), id->idInsOpt(), true);
            emitDispSveReg(id->idReg3(), id->idInsOpt(), true);
            emitDispSveExtendOptsModN(INS_OPTS_SXTW, emitGetInsSC(id));
            printf("]");
            break;

        // <Zd>.D, [<Zn>.D, <Zm>.D, UXTW{ <amount>}]
        case IF_SVE_BH_3B_A: // ...........mmmmm ....hhnnnnnddddd -- SVE address generation
            emitDispSveReg(id->idReg1(), id->idInsOpt(), true); // ddddd
            printf("[");
            emitDispSveReg(id->idReg2(), id->idInsOpt(), true);
            emitDispSveReg(id->idReg3(), id->idInsOpt(), true);
            emitDispSveExtendOptsModN(INS_OPTS_UXTW, emitGetInsSC(id));
            printf("]");
            break;

        // <Zdn>.<T>, <V><m>
        case IF_SVE_CC_2A: // ........xx...... ......mmmmmddddd -- SVE insert SIMD&FP scalar register
            emitDispSveReg(id->idReg1(), id->idInsOpt(), true);                  // ddddd
            emitDispReg(id->idReg2(), optGetSveElemsize(id->idInsOpt()), false); // mmmmm
            break;

        // <Zdn>.<T>, <R><m>
        case IF_SVE_CD_2A: // ........xx...... ......mmmmmddddd -- SVE insert general register
            emitDispSveReg(id->idReg1(), id->idInsOpt(), true);                                            // ddddd
            emitDispReg(id->idReg2(), id->idInsOpt() == INS_OPTS_SCALABLE_D ? EA_8BYTE : EA_4BYTE, false); // mmmmm
            break;

        // <Pd>.H, <Pn>.B
        case IF_SVE_CK_2A: // ................ .......NNNN.DDDD -- SVE unpack predicate elements
            emitDispPredicateReg(id->idReg1(), insGetPredicateType(fmt), INS_OPTS_SCALABLE_H, true);  // DDDD
            emitDispPredicateReg(id->idReg2(), insGetPredicateType(fmt), INS_OPTS_SCALABLE_B, false); // NNNN
            break;

        // <Zdn>.<T>, <Pg>, <Zdn>.<T>, <Zm>.<T>
        case IF_SVE_CM_3A: // ........xx...... ...gggmmmmmddddd -- SVE conditionally broadcast element to vector
            emitDispSveReg(id->idReg1(), id->idInsOpt(), true);                                    // ddddd
            emitDispLowPredicateReg(id->idReg2(), insGetPredicateType(fmt), id->idInsOpt(), true); // ggg
            emitDispSveReg(id->idReg1(), id->idInsOpt(), true);                                    // ddddd
            emitDispSveReg(id->idReg3(), id->idInsOpt(), false);                                   // mmmmm
            break;

        // <V><dn>, <Pg>, <V><dn>, <Zm>.<T>
        // <R><dn>, <Pg>, <R><dn>, <Zm>.<T>
        case IF_SVE_CN_3A: // ........xx...... ...gggmmmmmddddd -- SVE conditionally extract element to SIMD&FP scalar
        case IF_SVE_CO_3A: // ........xx...... ...gggmmmmmddddd -- SVE conditionally extract element to general register
        case IF_SVE_HJ_3A: // ........xx...... ...gggmmmmmddddd -- SVE floating-point serial reduction (predicated)
            emitDispReg(id->idReg1(), size, true);                                                 // ddddd
            emitDispLowPredicateReg(id->idReg2(), insGetPredicateType(fmt), id->idInsOpt(), true); // ggg
            emitDispReg(id->idReg1(), size, true);                                                 // ddddd
            emitDispSveReg(id->idReg3(), id->idInsOpt(), false);                                   // mmmmm
            break;

        // <V><d>, <Pg>, <Zn>.<T>
        // <R><d>, <Pg>, <Zn>.<T>
        case IF_SVE_AF_3A: // ........xx...... ...gggnnnnnddddd -- SVE bitwise logical reduction (predicated)
        case IF_SVE_AK_3A: // ........xx...... ...gggnnnnnddddd -- SVE integer min/max reduction (predicated)
        case IF_SVE_CR_3A: // ........xx...... ...gggnnnnnddddd -- SVE extract element to SIMD&FP scalar register
        case IF_SVE_CS_3A: // ........xx...... ...gggnnnnnddddd -- SVE extract element to general register
        case IF_SVE_HE_3A: // ........xx...... ...gggnnnnnddddd -- SVE floating-point recursive reduction
            emitDispReg(id->idReg1(), size, true);                                              // ddddd
            emitDispPredicateReg(id->idReg2(), insGetPredicateType(fmt), id->idInsOpt(), true); // ggg
            emitDispSveReg(id->idReg3(), id->idInsOpt(), false);                                // mmmmm
            break;

        // <Vd>.<T>, <Pg>, <Zn>.<Tb>
        case IF_SVE_AG_3A: // ........xx...... ...gggnnnnnddddd -- SVE bitwise logical reduction (quadwords)
        case IF_SVE_AJ_3A: // ........xx...... ...gggnnnnnddddd -- SVE integer add reduction (quadwords)
        case IF_SVE_AL_3A: // ........xx...... ...gggnnnnnddddd -- SVE integer min/max reduction (quadwords)
        case IF_SVE_GS_3A: // ........xx...... ...gggnnnnnddddd -- SVE floating-point recursive reduction (quadwords)
            emitDispVectorReg(id->idReg1(), optSveToQuadwordElemsizeArrangement(id->idInsOpt()), true); // ddddd
            emitDispPredicateReg(id->idReg2(), insGetPredicateType(fmt), id->idInsOpt(), true);         // ggg
            emitDispSveReg(id->idReg3(), id->idInsOpt(), false);                                        // mmmmm
            break;

        // <Dd>, <Pg>, <Zn>.<T>
        case IF_SVE_AI_3A: // ........xx...... ...gggnnnnnddddd -- SVE integer add reduction (predicated)
            emitDispReg(id->idReg1(), EA_8BYTE, true);                                          // ddddd
            emitDispPredicateReg(id->idReg2(), insGetPredicateType(fmt), id->idInsOpt(), true); // ggg
            emitDispSveReg(id->idReg3(), id->idInsOpt(), false);                                // mmmmm
            break;

        // <Zd>.<T>, <Pg>/M, <Zn>.<T>
        case IF_SVE_AP_3A: // ........xx...... ...gggnnnnnddddd -- SVE bitwise unary operations (predicated)
        case IF_SVE_AQ_3A: // ........xx...... ...gggnnnnnddddd -- SVE integer unary operations (predicated)
        case IF_SVE_CU_3A: // ........xx...... ...gggnnnnnddddd -- SVE reverse within elements
        case IF_SVE_ES_3A: // ........xx...... ...gggnnnnnddddd -- SVE2 integer unary operations (predicated)
        case IF_SVE_HQ_3A: // ........xx...... ...gggnnnnnddddd -- SVE floating-point round to integral value
        case IF_SVE_HR_3A: // ........xx...... ...gggnnnnnddddd -- SVE floating-point unary operations
            emitDispSveReg(id->idReg1(), id->idInsOpt(), true);                                 // ddddd
            emitDispPredicateReg(id->idReg2(), insGetPredicateType(fmt), id->idInsOpt(), true); // ggg
            emitDispSveReg(id->idReg3(), id->idInsOpt(), false);                                // mmmmm
            break;

        case IF_SVE_CE_2A: // ................ ......nnnnn.DDDD -- SVE move predicate from vector
            emitDispPredicateReg(id->idReg1(), insGetPredicateType(fmt), INS_OPTS_SCALABLE_B, true); // DDDD
            emitDispSveReg(id->idReg2(), false);                                                     // nnnnn
            break;
        case IF_SVE_CE_2B: // .........i...ii. ......nnnnn.DDDD -- SVE move predicate from vector
            emitDispPredicateReg(id->idReg1(), insGetPredicateType(fmt), INS_OPTS_SCALABLE_D, true); // DDDD
            emitDispSveRegIndex(id->idReg2(), emitGetInsSC(id), false);                              // nnnnn
            break;
        case IF_SVE_CE_2C: // ..............i. ......nnnnn.DDDD -- SVE move predicate from vector
            emitDispPredicateReg(id->idReg1(), insGetPredicateType(fmt), INS_OPTS_SCALABLE_H, true); // DDDD
            emitDispSveRegIndex(id->idReg2(), emitGetInsSC(id), false);                              // nnnnn
            break;
        case IF_SVE_CE_2D: // .............ii. ......nnnnn.DDDD -- SVE move predicate from vector
            emitDispPredicateReg(id->idReg1(), insGetPredicateType(fmt), INS_OPTS_SCALABLE_S, true); // DDDD
            emitDispSveRegIndex(id->idReg2(), emitGetInsSC(id), false);                              // nnnnn
            break;
        case IF_SVE_CF_2A:                      // ................ .......NNNNddddd -- SVE move predicate into vector
            emitDispSveReg(id->idReg1(), true); // ddddd
            emitDispPredicateReg(id->idReg2(), insGetPredicateType(fmt), INS_OPTS_SCALABLE_B, false); // NNNN
            break;
        case IF_SVE_CF_2B: // .........i...ii. .......NNNNddddd -- SVE move predicate into vector
            emitDispSveRegIndex(id->idReg1(), emitGetInsSC(id), true);                                // ddddd
            emitDispPredicateReg(id->idReg2(), insGetPredicateType(fmt), INS_OPTS_SCALABLE_D, false); // NNNN
            break;
        case IF_SVE_CF_2C: // ..............i. .......NNNNddddd -- SVE move predicate into vector
            emitDispSveRegIndex(id->idReg1(), emitGetInsSC(id), true);                                // ddddd
            emitDispPredicateReg(id->idReg2(), insGetPredicateType(fmt), INS_OPTS_SCALABLE_H, false); // NNNN
            break;
        case IF_SVE_CF_2D: // .............ii. .......NNNNddddd -- SVE move predicate into vector
            emitDispSveRegIndex(id->idReg1(), emitGetInsSC(id), true);                                // ddddd
            emitDispPredicateReg(id->idReg2(), insGetPredicateType(fmt), INS_OPTS_SCALABLE_S, false); // NNNN
            break;

        // <Pd>.<T>, <Pn>.<T>, <Pm>.<T>
        case IF_SVE_CI_3A: // ........xx..MMMM .......NNNN.DDDD -- SVE permute predicate elements
            emitDispPredicateReg(id->idReg1(), insGetPredicateType(fmt, 1), id->idInsOpt(), true);  // DDDD
            emitDispPredicateReg(id->idReg2(), insGetPredicateType(fmt, 2), id->idInsOpt(), true);  // NNNN
            emitDispPredicateReg(id->idReg3(), insGetPredicateType(fmt, 3), id->idInsOpt(), false); // MMMM
            break;

        // <Zd>.<T>, <Pg>, <Zn>.<T>
        case IF_SVE_CL_3A: // ........xx...... ...gggnnnnnddddd -- SVE compress active elements
            emitDispSveReg(id->idReg1(), id->idInsOpt(), true);                                 // ddddd
            emitDispPredicateReg(id->idReg2(), insGetPredicateType(fmt), id->idInsOpt(), true); // ggg
            emitDispSveReg(id->idReg3(), id->idInsOpt(), false);                                // mmmmm
            break;

        // <Zd>.<T>, <Pg>/M, <V><n>
        case IF_SVE_CP_3A: // ........xx...... ...gggnnnnnddddd -- SVE copy SIMD&FP scalar register to vector
                           // (predicated)
            emitDispSveReg(id->idReg1(), id->idInsOpt(), true);                                 // ddddd
            emitDispPredicateReg(id->idReg2(), insGetPredicateType(fmt), id->idInsOpt(), true); // ggg
            emitDispReg(id->idReg3(), size, false);                                             // mmmmm
            break;

        // <Zd>.<T>, <Pg>/M, <R><n|SP>
        case IF_SVE_CQ_3A: // ........xx...... ...gggnnnnnddddd -- SVE copy general register to vector (predicated)
            emitDispSveReg(id->idReg1(), id->idInsOpt(), true);                                 // ddddd
            emitDispPredicateReg(id->idReg2(), insGetPredicateType(fmt), id->idInsOpt(), true); // ggg
            emitDispReg(encodingZRtoSP(id->idReg3()), size, false);                             // mmmmm
            break;

        // <Zd>.Q, <Pg>/M, <Zn>.Q
        case IF_SVE_CT_3A: // ................ ...gggnnnnnddddd -- SVE reverse doublewords
            emitDispSveReg(id->idReg1(), INS_OPTS_SCALABLE_Q, true);                            // ddddd
            emitDispPredicateReg(id->idReg2(), insGetPredicateType(fmt), id->idInsOpt(), true); // ggg
            emitDispSveReg(id->idReg3(), INS_OPTS_SCALABLE_Q, false);                           // nnnnn
            break;

        // <Zd>.<T>, <Pv>, {<Zn1>.<T>, <Zn2>.<T>}
        case IF_SVE_CV_3A: // ........xx...... ...VVVnnnnnddddd -- SVE vector splice (constructive)
            emitDispSveReg(id->idReg1(), id->idInsOpt(), true);                                             // ddddd
            emitDispPredicateReg(id->idReg2(), insGetPredicateType(fmt), id->idInsOpt(), true);             // VVV
            emitDispSveConsecutiveRegList(id->idReg3(), insGetSveReg1ListSize(ins), id->idInsOpt(), false); // nnnnn
            break;

        // <Zdn>.<T>, <Pv>, <Zdn>.<T>, <Zm>.<T>
        case IF_SVE_CV_3B: // ........xx...... ...VVVmmmmmddddd -- SVE vector splice (destructive)
            emitDispSveReg(id->idReg1(), id->idInsOpt(), true);                                 // ddddd
            emitDispPredicateReg(id->idReg2(), insGetPredicateType(fmt), id->idInsOpt(), true); // VVV
            emitDispSveReg(id->idReg1(), id->idInsOpt(), true);                                 // ddddd
            emitDispSveReg(id->idReg3(), id->idInsOpt(), false);                                // mmmmm
            break;

        // MOV <Zd>.<T>, <Pv>/M, <Zn>.<T> or SEL <Zd>.<T>, <Pv>, <Zn>.<T>, <Zm>.<T>
        case IF_SVE_CW_4A: // ........xx.mmmmm ..VVVVnnnnnddddd -- SVE select vector elements (predicated)
        {
            emitDispSveReg(id->idReg1(), id->idInsOpt(), true); // ddddd

            if (id->idIns() == INS_sve_mov)
            {
                emitDispPredicateReg(id->idReg2(), PREDICATE_MERGE, id->idInsOpt(), true); // VVVV
                emitDispSveReg(id->idReg3(), id->idInsOpt(), false);                       // nnnnn
            }
            else
            {
                emitDispPredicateReg(id->idReg2(), PREDICATE_NONE, id->idInsOpt(), true); // VVVV
                emitDispSveReg(id->idReg3(), id->idInsOpt(), true);                       // nnnnn
                emitDispSveReg(id->idReg4(), id->idInsOpt(), false);                      // mmmmm
            }
            break;
        }

        // <Pd>.<T>, <Pg>/Z, <Zn>.<T>, <Zm>.<T>
        case IF_SVE_CX_4A: // ........xx.mmmmm ...gggnnnnn.DDDD -- SVE integer compare vectors
        case IF_SVE_GE_4A: // ........xx.mmmmm ...gggnnnnn.DDDD -- SVE2 character match
        case IF_SVE_HT_4A: // ........xx.mmmmm ...gggnnnnn.DDDD -- SVE floating-point compare vectors
            emitDispPredicateReg(id->idReg1(), insGetPredicateType(fmt, 1), id->idInsOpt(), true); // DDDD
            emitDispPredicateReg(id->idReg2(), insGetPredicateType(fmt, 2), id->idInsOpt(), true); // ggg
            emitDispSveReg(id->idReg3(), id->idInsOpt(), true);                                    // nnnnn
            emitDispSveReg(id->idReg4(), id->idInsOpt(), false);                                   // mmmmm
            break;

        // <Pd>.<T>, <Pg>/Z, <Zn>.<T>, <Zm>.D
        case IF_SVE_CX_4A_A: // ........xx.mmmmm ...gggnnnnn.DDDD -- SVE integer compare vectors
            emitDispPredicateReg(id->idReg1(), insGetPredicateType(fmt, 1), id->idInsOpt(), true); // DDDD
            emitDispPredicateReg(id->idReg2(), insGetPredicateType(fmt, 2), id->idInsOpt(), true); // ggg
            emitDispSveReg(id->idReg3(), id->idInsOpt(), true);                                    // nnnnn
            emitDispSveReg(id->idReg4(), INS_OPTS_SCALABLE_D, false);                              // mmmmm
            break;

        // <Pd>.<T>, <Pg>/Z, <Zn>.<T>, #<imm>
        case IF_SVE_CY_3A: // ........xx.iiiii ...gggnnnnn.DDDD -- SVE integer compare with signed immediate
        case IF_SVE_CY_3B: // ........xx.iiiii ii.gggnnnnn.DDDD -- SVE integer compare with unsigned immediate
            emitDispPredicateReg(id->idReg1(), insGetPredicateType(fmt, 1), id->idInsOpt(), true); // DDDD
            emitDispPredicateReg(id->idReg2(), insGetPredicateType(fmt, 2), id->idInsOpt(), true); // ggg
            emitDispSveReg(id->idReg3(), id->idInsOpt(), true);                                    // nnnnn
            emitDispImm(emitGetInsSC(id), false, (fmt == IF_SVE_CY_3B));                           // iiiii
            break;

        // <Zda>.S, <Zn>.H, <Zm>.H[<imm>]
        case IF_SVE_EG_3A: // ...........iimmm ......nnnnnddddd -- SVE two-way dot product (indexed)
        case IF_SVE_FG_3A: // ...........iimmm ....i.nnnnnddddd -- SVE2 integer multiply-add long (indexed)
        case IF_SVE_FJ_3A: // ...........iimmm ....i.nnnnnddddd -- SVE2 saturating multiply-add (indexed)
        case IF_SVE_GZ_3A: // ...........iimmm ....i.nnnnnddddd -- SVE floating-point multiply-add long (indexed)
        // <Zda>.S, <Zn>.B, <Zm>.B[<imm>]
        case IF_SVE_EY_3A:   // ...........iimmm ......nnnnnddddd -- SVE integer dot product (indexed)
        case IF_SVE_EZ_3A:   // ...........iimmm ......nnnnnddddd -- SVE mixed sign dot product (indexed)
        case IF_SVE_GY_3B_D: // ...........iimmm ......nnnnnddddd -- SVE BFloat16 floating-point dot product (indexed)
        // <Zd>.S, <Zn>.H, <Zm>.H[<imm>]
        case IF_SVE_FE_3A: // ...........iimmm ....i.nnnnnddddd -- SVE2 integer multiply long (indexed)
        case IF_SVE_FH_3A: // ...........iimmm ....i.nnnnnddddd -- SVE2 saturating multiply (indexed)
        case IF_SVE_GY_3B: // ...........iimmm ......nnnnnddddd -- SVE BFloat16 floating-point dot product (indexed)
        // <Zda>.S, <Zn>.S, <Zm>.S[<imm>]
        case IF_SVE_GU_3A: // ...........iimmm ......nnnnnddddd -- SVE floating-point multiply-add (indexed)
        case IF_SVE_GX_3A: // ...........iimmm ......nnnnnddddd -- SVE floating-point multiply (indexed)
        case IF_SVE_FF_3B: // ...........iimmm ......nnnnnddddd -- SVE2 integer multiply-add (indexed)
        case IF_SVE_FK_3B: // ...........iimmm ......nnnnnddddd -- SVE2 saturating multiply-add high (indexed)
            emitDispSveReg(id->idReg1(), INS_OPTS_SCALABLE_S, true); // ddddd
            emitDispSveReg(id->idReg2(), id->idInsOpt(), true);      // nnnnn
            emitDispSveReg(id->idReg3(), id->idInsOpt(), false);     // mmm
            emitDispElementIndex(emitGetInsSC(id), false);           // ii/iii
            break;

        // <Zda>.S, <Zn>.H, <Zm>.H
        case IF_SVE_EF_3A: // ...........mmmmm ......nnnnnddddd -- SVE two-way dot product
        case IF_SVE_HA_3A: // ...........mmmmm ......nnnnnddddd -- SVE BFloat16 floating-point dot product
        case IF_SVE_HB_3A: // ...........mmmmm ......nnnnnddddd -- SVE floating-point multiply-add long
        case IF_SVE_HD_3A: // ...........mmmmm ......nnnnnddddd -- SVE floating point matrix multiply accumulate
        case IF_SVE_EI_3A: // ...........mmmmm ......nnnnnddddd -- SVE mixed sign dot product
        case IF_SVE_GO_3A: // ...........mmmmm ......nnnnnddddd -- SVE2 FP8 multiply-add long long
            emitDispSveReg(id->idReg1(), INS_OPTS_SCALABLE_S, true); // ddddd
            emitDispSveReg(id->idReg2(), id->idInsOpt(), true);      // nnnnn
            emitDispSveReg(id->idReg3(), id->idInsOpt(), false);     // mmmmm
            break;

        // <Zda>.S, <Zn>.B, <Zm>.B
        case IF_SVE_HA_3A_F: // ...........mmmmm ......nnnnnddddd -- SVE BFloat16 floating-point dot product
            emitDispSveReg(id->idReg1(), INS_OPTS_SCALABLE_S, true);  // ddddd
            emitDispSveReg(id->idReg2(), INS_OPTS_SCALABLE_B, true);  // nnnnn
            emitDispSveReg(id->idReg3(), INS_OPTS_SCALABLE_B, false); // mmmmm
            break;

        // <Zd>.D, <Zn>.S, <Zm>.S[<imm>]
        case IF_SVE_FE_3B: // ...........immmm ....i.nnnnnddddd -- SVE2 integer multiply long (indexed)
        case IF_SVE_FH_3B: // ...........immmm ....i.nnnnnddddd -- SVE2 saturating multiply (indexed)
        // <Zda>.D, <Zn>.S, <Zm>.S[<imm>]
        case IF_SVE_FG_3B: // ...........immmm ....i.nnnnnddddd -- SVE2 integer multiply-add long (indexed)
        case IF_SVE_FJ_3B: // ...........immmm ....i.nnnnnddddd -- SVE2 saturating multiply-add (indexed)
            emitDispSveReg(id->idReg1(), INS_OPTS_SCALABLE_D, true); // ddddd
            emitDispSveReg(id->idReg2(), id->idInsOpt(), true);      // nnnnn
            emitDispSveReg(id->idReg3(), id->idInsOpt(), false);     // mmmm
            emitDispElementIndex(emitGetInsSC(id), false);           // ii
            break;

        // <Zda>.D, <Zn>.H, <Zm>.H[<imm>]
        case IF_SVE_EY_3B: // ...........immmm ......nnnnnddddd -- SVE integer dot product (indexed)
            emitDispSveReg(id->idReg1(), INS_OPTS_SCALABLE_D, true); // ddddd
            emitDispSveReg(id->idReg2(), id->idInsOpt(), true);      // nnnnn
            emitDispSveReg(id->idReg3(), id->idInsOpt(), false);     // mmm
            emitDispElementIndex(emitGetInsSC(id), false);           // ii
            break;

        // <Zda>.H, <Zn>.B, <Zm>.B[<imm>]
        case IF_SVE_GY_3A: // ...........iimmm ....i.nnnnnddddd -- SVE BFloat16 floating-point dot product (indexed)
            emitDispSveReg(id->idReg1(), INS_OPTS_SCALABLE_H, true); // ddddd
            emitDispSveReg(id->idReg2(), id->idInsOpt(), true);      // nnnnn
            emitDispSveReg(id->idReg3(), id->idInsOpt(), false);     // mmm
            emitDispElementIndex(emitGetInsSC(id), false);           // iii
            break;

        // <Zd>.H, <Zn>.H, <Zm>.H[<imm>]
        case IF_SVE_FD_3A: // .........i.iimmm ......nnnnnddddd -- SVE2 integer multiply (indexed)
        case IF_SVE_FI_3A: // .........i.iimmm ......nnnnnddddd -- SVE2 saturating multiply high (indexed)
        // <Zd>.S, <Zn>.S, <Zm>.S[<imm>]
        case IF_SVE_FD_3B: // ...........iimmm ......nnnnnddddd -- SVE2 integer multiply (indexed)
        case IF_SVE_FI_3B: // ...........iimmm ......nnnnnddddd -- SVE2 saturating multiply high (indexed)
        // <Zd>.D, <Zn>.D, <Zm>.D[<imm>]
        case IF_SVE_FD_3C: // ...........immmm ......nnnnnddddd -- SVE2 integer multiply (indexed)
        case IF_SVE_FI_3C: // ...........immmm ......nnnnnddddd -- SVE2 saturating multiply high (indexed)
        // <Zda>.D, <Zn>.D, <Zm>.D[<imm>]
        case IF_SVE_GU_3B: // ...........immmm ......nnnnnddddd -- SVE floating-point multiply-add (indexed)
        case IF_SVE_GX_3B: // ...........immmm ......nnnnnddddd -- SVE floating-point multiply (indexed)
        case IF_SVE_FF_3C: // ...........immmm ......nnnnnddddd -- SVE2 integer multiply-add (indexed)
        case IF_SVE_FK_3C: // ...........immmm ......nnnnnddddd -- SVE2 saturating multiply-add high (indexed)
        // <Zda>.H, <Zn>.H, <Zm>.H[<imm>]
        case IF_SVE_GU_3C: // .........i.iimmm ......nnnnnddddd -- SVE floating-point multiply-add (indexed)
        case IF_SVE_GX_3C: // .........i.iimmm ......nnnnnddddd -- SVE floating-point multiply (indexed)
        case IF_SVE_FF_3A: // .........i.iimmm ......nnnnnddddd -- SVE2 integer multiply-add (indexed)
        case IF_SVE_FK_3A: // .........i.iimmm ......nnnnnddddd -- SVE2 saturating multiply-add high (indexed)
            emitDispSveReg(id->idReg1(), id->idInsOpt(), true);  // ddddd
            emitDispSveReg(id->idReg2(), id->idInsOpt(), true);  // nnnnn
            emitDispSveReg(id->idReg3(), id->idInsOpt(), false); // mmm
            emitDispElementIndex(emitGetInsSC(id), false);       // i/ii/iii
            break;

        // <Pd>.B, <Pg>/Z, <Pn>.B, <Pm>.B
        case IF_SVE_CZ_4A: // ............MMMM ..gggg.NNNN.DDDD -- SVE predicate logical operations
        {
            bool isFourReg =
                !((ins == INS_sve_mov) || (ins == INS_sve_movs) || (ins == INS_sve_not) || (ins == INS_sve_nots));
            PredicateType ptype = (ins == INS_sve_sel) ? PREDICATE_NONE : insGetPredicateType(fmt, 2);
            emitDispPredicateReg(id->idReg1(), insGetPredicateType(fmt, 1), id->idInsOpt(), true);      // DDDD
            emitDispPredicateReg(id->idReg2(), ptype, id->idInsOpt(), true);                            // gggg
            emitDispPredicateReg(id->idReg3(), insGetPredicateType(fmt, 3), id->idInsOpt(), isFourReg); // NNNN

            if (isFourReg)
            {
                emitDispPredicateReg(id->idReg4(), insGetPredicateType(fmt, 4), id->idInsOpt(), false); // MMMM
            }

            break;
        }

        // <Pd>.B, <Pn>.B
        case IF_SVE_CZ_4A_A: // ............MMMM ..gggg.NNNN.DDDD -- SVE predicate logical operations
        case IF_SVE_CZ_4A_L: // ............MMMM ..gggg.NNNN.DDDD -- SVE predicate logical operations
            emitDispPredicateReg(id->idReg1(), insGetPredicateType(fmt, 1), id->idInsOpt(), true);  // DDDD
            emitDispPredicateReg(id->idReg2(), insGetPredicateType(fmt, 2), id->idInsOpt(), false); // NNNN
            break;

        //  <Pd>.B, <Pg>/M, <Pn>.B
        case IF_SVE_CZ_4A_K: // ............MMMM ..gggg.NNNN.DDDD -- SVE predicate logical operations
            emitDispPredicateReg(id->idReg1(), insGetPredicateType(fmt, 1), id->idInsOpt(), true);  // DDDD
            emitDispPredicateReg(id->idReg2(), insGetPredicateType(fmt, 2), id->idInsOpt(), true);  // gggg
            emitDispPredicateReg(id->idReg3(), insGetPredicateType(fmt, 3), id->idInsOpt(), false); // NNNN
            break;

        //  <Pd>.B, <Pg>/Z, <Pn>.B, <Pm>.B
        case IF_SVE_DA_4A: // ............MMMM ..gggg.NNNN.DDDD -- SVE propagate break from previous partition
        {
            emitDispPredicateReg(id->idReg1(), insGetPredicateType(fmt, 1), id->idInsOpt(), true);  // DDDD
            emitDispPredicateReg(id->idReg2(), insGetPredicateType(fmt, 2), id->idInsOpt(), true);  // gggg
            emitDispPredicateReg(id->idReg3(), insGetPredicateType(fmt, 3), id->idInsOpt(), true);  // NNNN
            emitDispPredicateReg(id->idReg4(), insGetPredicateType(fmt, 4), id->idInsOpt(), false); // MMMM
            break;
        }

        // <Pd>.B, <Pg>/<ZM>, <Pn>.B
        case IF_SVE_DB_3A: // ................ ..gggg.NNNNMDDDD -- SVE partition break condition
        case IF_SVE_DB_3B: // ................ ..gggg.NNNN.DDDD -- SVE partition break condition
        {
            PredicateType ptype = (id->idPredicateReg2Merge()) ? PREDICATE_MERGE : PREDICATE_ZERO;
            emitDispPredicateReg(id->idReg1(), insGetPredicateType(fmt, 1), id->idInsOpt(), true);  // DDDD
            emitDispPredicateReg(id->idReg2(), ptype, id->idInsOpt(), true);                        // gggg
            emitDispPredicateReg(id->idReg3(), insGetPredicateType(fmt, 3), id->idInsOpt(), false); // NNNN
            break;
        }

        // <Pdm>.B, <Pg>/Z, <Pn>.B, <Pdm>.B
        case IF_SVE_DC_3A: // ................ ..gggg.NNNN.MMMM -- SVE propagate break to next partition
            emitDispPredicateReg(id->idReg1(), insGetPredicateType(fmt, 1), id->idInsOpt(), true);  // DDDD
            emitDispPredicateReg(id->idReg2(), insGetPredicateType(fmt, 2), id->idInsOpt(), true);  // gggg
            emitDispPredicateReg(id->idReg3(), insGetPredicateType(fmt, 3), id->idInsOpt(), true);  // NNNN
            emitDispPredicateReg(id->idReg1(), insGetPredicateType(fmt, 4), id->idInsOpt(), false); // MMMM
            break;

        // <Pdn>.B, <Pg>, <Pdn>.B
        case IF_SVE_DD_2A: // ................ .......gggg.DDDD -- SVE predicate first active
            emitDispPredicateReg(id->idReg1(), insGetPredicateType(fmt, 1), id->idInsOpt(), true);  // DDDD
            emitDispPredicateReg(id->idReg2(), insGetPredicateType(fmt, 2), id->idInsOpt(), true);  // gggg
            emitDispPredicateReg(id->idReg1(), insGetPredicateType(fmt, 3), id->idInsOpt(), false); // DDDD
            break;

        // <Pd>.<T>{, <pattern>}
        case IF_SVE_DE_1A: // ........xx...... ......ppppp.DDDD -- SVE predicate initialize
        {
            bool dispPattern = (id->idSvePattern() != SVE_PATTERN_ALL);
            emitDispPredicateReg(id->idReg1(), insGetPredicateType(fmt), id->idInsOpt(), dispPattern); // DDDD
            if (dispPattern)
            {
                emitDispSvePattern(id->idSvePattern(), false); // ppppp
            }
            break;
        }

        // <Pd>.<T>, <Pn>.<T>
        case IF_SVE_CJ_2A: // ........xx...... .......NNNN.DDDD -- SVE reverse predicate elements
            emitDispPredicateReg(id->idReg1(), insGetPredicateType(fmt, 1), id->idInsOpt(), true);  // DDDD
            emitDispPredicateReg(id->idReg2(), insGetPredicateType(fmt, 2), id->idInsOpt(), false); // NNNN
            break;

        // <Pdn>.<T>, <Pv>, <Pdn>.<T>
        case IF_SVE_DF_2A: // ........xx...... .......VVVV.DDDD -- SVE predicate next active
            emitDispPredicateReg(id->idReg1(), insGetPredicateType(fmt, 1), id->idInsOpt(), true);  // DDDD
            emitDispPredicateReg(id->idReg2(), insGetPredicateType(fmt, 2), id->idInsOpt(), true);  // VVVV
            emitDispPredicateReg(id->idReg1(), insGetPredicateType(fmt, 3), id->idInsOpt(), false); // DDDD
            break;

        // <Pd>.B, <Pg>/Z
        case IF_SVE_DG_2A: // ................ .......gggg.DDDD -- SVE predicate read from FFR (predicated)
        case IF_SVE_DI_2A: // ................ ..gggg.NNNN..... -- SVE predicate test
            emitDispPredicateReg(id->idReg1(), insGetPredicateType(fmt, 1), id->idInsOpt(), true);  // DDDD
            emitDispPredicateReg(id->idReg2(), insGetPredicateType(fmt, 2), id->idInsOpt(), false); // gggg
            break;

        // <Pd>.B
        case IF_SVE_DH_1A: // ................ ............DDDD -- SVE predicate read from FFR (unpredicated)
        case IF_SVE_DJ_1A: // ................ ............DDDD -- SVE predicate zero
            emitDispPredicateReg(id->idReg1(), insGetPredicateType(fmt), id->idInsOpt(), false); // DDDD
            break;

        // <Xd>, <Pg>, <Pn>.<T>
        case IF_SVE_DK_3A:                             // ........xx...... ..gggg.NNNNddddd -- SVE predicate count
            emitDispReg(id->idReg1(), EA_8BYTE, true); // ddddd
            emitDispPredicateReg(id->idReg2(), insGetPredicateType(fmt, 2), id->idInsOpt(), true);  // gggg
            emitDispPredicateReg(id->idReg3(), insGetPredicateType(fmt, 3), id->idInsOpt(), false); // NNNN
            break;

        // <Zda>.<T>, <Pg>/M, <Zn>.<Tb>
        case IF_SVE_EQ_3A: // ........xx...... ...gggnnnnnddddd -- SVE2 integer pairwise add and accumulate long
            emitDispSveReg(id->idReg1(), id->idInsOpt(), true);                                    // ddddd
            emitDispLowPredicateReg(id->idReg2(), insGetPredicateType(fmt), id->idInsOpt(), true); // ggg
            emitDispSveReg(id->idReg3(), (insOpts)((unsigned)id->idInsOpt() - 1), false);          // mmmmm
            break;

        // <Zd>.H, { <Zn1>.S-<Zn2>.S }, #<const>
        case IF_SVE_GA_2A: // ............iiii ......nnnn.ddddd -- SME2 multi-vec shift narrow
            emitDispSveReg(id->idReg1(), id->idInsOpt(), true);                        // ddddd
            emitDispSveConsecutiveRegList(id->idReg2(), 2, INS_OPTS_SCALABLE_S, true); // nnnn
            emitDispImm(emitGetInsSC(id), false);                                      // iiii
            break;

        // <Xd>, <PNn>.<T>, <vl>
        case IF_SVE_DL_2A: // ........xx...... .....l.NNNNddddd -- SVE predicate count (predicate-as-counter)
            emitDispReg(id->idReg1(), EA_8BYTE, true);                                          // ddddd
            emitDispPredicateReg(id->idReg2(), insGetPredicateType(fmt), id->idInsOpt(), true); // NNNN
            emitDispVectorLengthSpecifier(id);
            break;

        // <Xdn>, <Pm>.<T>
        case IF_SVE_DM_2A: // ........xx...... .......MMMMddddd -- SVE inc/dec register by predicate count
            emitDispReg(id->idReg1(), id->idOpSize(), true);                                     // ddddd
            emitDispPredicateReg(id->idReg2(), insGetPredicateType(fmt), id->idInsOpt(), false); // MMMM
            break;

        // <Zdn>.<T>, <Pm>.<T>
        case IF_SVE_DN_2A: // ........xx...... .......MMMMddddd -- SVE inc/dec vector by predicate count
        case IF_SVE_DP_2A: // ........xx...... .......MMMMddddd -- SVE saturating inc/dec vector by predicate count
            emitDispSveReg(id->idReg1(), id->idInsOpt(), true);                                  // ddddd
            emitDispPredicateReg(id->idReg2(), insGetPredicateType(fmt), id->idInsOpt(), false); // MMMM
            break;

        // <Xdn>, <Pm>.<T>, <Wdn>
        // <Xdn>, <Pm>.<T>
        case IF_SVE_DO_2A: // ........xx...... .....X.MMMMddddd -- SVE saturating inc/dec register by predicate count
            if ((ins == INS_sve_sqdecp) || (ins == INS_sve_sqincp))
            {
                // 32-bit result: <Xdn>, <Pm>.<T>, <Wdn>
                // 64-bit result: <Xdn>, <Pm>.<T>
                const bool is32BitResult = (id->idOpSize() == EA_4BYTE);                                     // X
                emitDispReg(id->idReg1(), EA_8BYTE, true);                                                   // ddddd
                emitDispPredicateReg(id->idReg2(), insGetPredicateType(fmt), id->idInsOpt(), is32BitResult); // MMMM

                if (is32BitResult)
                {
                    emitDispReg(id->idReg1(), EA_4BYTE, false);
                }
            }
            else
            {
                assert((ins == INS_sve_uqdecp) || (ins == INS_sve_uqincp));
                emitDispReg(id->idReg1(), id->idOpSize(), true);                                     // ddddd
                emitDispPredicateReg(id->idReg2(), insGetPredicateType(fmt), id->idInsOpt(), false); // MMMM
            }
            break;

        // none
        case IF_SVE_DQ_0A: // ................ ................ -- SVE FFR initialise
            break;

        // <Pn>.B
        case IF_SVE_DR_1A: // ................ .......NNNN..... -- SVE FFR write from predicate
            emitDispPredicateReg(id->idReg1(), insGetPredicateType(fmt), id->idInsOpt(), false); // NNNN
            break;

        // <R><n>, <R><m>
        case IF_SVE_DS_2A: // .........x.mmmmm ......nnnnn..... -- SVE conditionally terminate scalars
            emitDispReg(id->idReg1(), id->idOpSize(), true);  // nnnnn
            emitDispReg(id->idReg2(), id->idOpSize(), false); // mmmmm
            break;

        // <Zd>.H, {<Zn1>.S-<Zn2>.S }
        case IF_SVE_FZ_2A: // ................ ......nnnn.ddddd -- SME2 multi-vec extract narrow
        {
            emitDispSveReg(id->idReg1(), INS_OPTS_SCALABLE_H, true);
            emitDispSveConsecutiveRegList(id->idReg2(), 2, INS_OPTS_SCALABLE_S, false);
            break;
        }

        // <Zd>.B, {<Zn1>.H-<Zn2>.H }
        case IF_SVE_HG_2A: // ................ ......nnnn.ddddd -- SVE2 FP8 downconverts
        {
            emitDispSveReg(id->idReg1(), INS_OPTS_SCALABLE_B, true);
            emitDispSveConsecutiveRegList(id->idReg2(), 2, INS_OPTS_SCALABLE_H, false);
            break;
        }

        // <Zd>.<T>, <Zn>.<Tb>
        case IF_SVE_GD_2A: // .........x.xx... ......nnnnnddddd -- SVE2 saturating extract narrow
            emitDispSveReg(id->idReg1(), id->idInsOpt(), true);                                  // ddddd
            emitDispSveReg(id->idReg2(), optWidenSveElemsizeArrangement(id->idInsOpt()), false); // nnnnn
            break;

        // <Xd|SP>, <Xn|SP>, #<imm>
        case IF_SVE_BB_2A: // ...........nnnnn .....iiiiiiddddd -- SVE stack frame adjustment
        {
            const regNumber reg1 = (id->idReg1() == REG_ZR) ? REG_SP : id->idReg1();
            const regNumber reg2 = (id->idReg2() == REG_ZR) ? REG_SP : id->idReg2();
            emitDispReg(reg1, id->idOpSize(), true); // ddddd
            emitDispReg(reg2, id->idOpSize(), true); // nnnnn
            emitDispImm(emitGetInsSC(id), false);    // iiiiii
            break;
        }

        // <Xd>, #<imm>
        case IF_SVE_BC_1A: // ................ .....iiiiiiddddd -- SVE stack frame size
            emitDispReg(id->idReg1(), id->idOpSize(), true); // ddddd
            emitDispImm(emitGetInsSC(id), false);            // iiiiii
            break;

        // <Zd>.<T>, <Zn>.<Tb>, #<const>
        case IF_SVE_FR_2A: // .........x.xxiii ......nnnnnddddd -- SVE2 bitwise shift left long
        {
            const insOpts largeSizeSpecifier = (insOpts)(id->idInsOpt() + 1);
            emitDispSveReg(id->idReg1(), largeSizeSpecifier, true); // ddddd
            emitDispSveReg(id->idReg2(), id->idInsOpt(), true);     // nnnnn
            emitDispImm(emitGetInsSC(id), false);                   // iii
            break;
        }

        // <Zd>.<T>, <Zn>.<Tb>, #<const>
        case IF_SVE_GB_2A: // .........x.xxiii ......nnnnnddddd -- SVE2 bitwise shift right narrow
        {
            const insOpts largeSizeSpecifier = (insOpts)(id->idInsOpt() + 1);
            emitDispSveReg(id->idReg1(), id->idInsOpt(), true);     // ddddd
            emitDispSveReg(id->idReg2(), largeSizeSpecifier, true); // nnnnn
            emitDispImm(emitGetInsSC(id), false);                   // iii
            break;
        }

        // <Zdn>.<T>, <Zdn>.<T>, <Zm>.<T>, <const>
        case IF_SVE_FV_2A: // ........xx...... .....rmmmmmddddd -- SVE2 complex integer add
        {
            // Rotation bit implies rotation is 270 if set, else rotation is 90
            const ssize_t rot = emitDecodeRotationImm90_or_270(emitGetInsSC(id));
            emitDispSveReg(id->idReg1(), id->idInsOpt(), true); // ddddd
            emitDispSveReg(id->idReg1(), id->idInsOpt(), true); // ddddd
            emitDispSveReg(id->idReg2(), id->idInsOpt(), true); // mmmmm
            emitDispImm(rot, false);                            // r
            break;
        }

        // <Zda>.<T>, <Zn>.<T>, <Zm>.<T>
        case IF_SVE_FY_3A: // .........x.mmmmm ......nnnnnddddd -- SVE2 integer add/subtract long with carry
            emitDispSveReg(id->idReg1(), id->idInsOpt(), true);  // ddddd
            emitDispSveReg(id->idReg2(), id->idInsOpt(), true);  // nnnnn
            emitDispSveReg(id->idReg3(), id->idInsOpt(), false); // mmmmm
            break;

        // <Zdn>.B, <Zdn>.B, <Zm>.B
        // <Zdn>.S, <Zdn>.S, <Zm>.S
        case IF_SVE_GK_2A: // ................ ......mmmmmddddd -- SVE2 crypto destructive binary operations
            emitDispSveReg(id->idReg1(), id->idInsOpt(), true);  // ddddd
            emitDispSveReg(id->idReg1(), id->idInsOpt(), true);  // ddddd
            emitDispSveReg(id->idReg2(), id->idInsOpt(), false); // mmmmm
            break;

        // <Zdn>.B, <Zdn>.B
        case IF_SVE_GL_1A: // ................ ...........ddddd -- SVE2 crypto unary operations
            emitDispSveReg(id->idReg1(), id->idInsOpt(), true);  // ddddd
            emitDispSveReg(id->idReg1(), id->idInsOpt(), false); // ddddd
            break;

        // <Pd>.<T>, <R><n>, <R><m>
        case IF_SVE_DT_3A: // ........xx.mmmmm ...X..nnnnn.DDDD -- SVE integer compare scalar count and limit
        // <Pd>.<T>, <Xn>, <Xm>
        case IF_SVE_DU_3A: // ........xx.mmmmm ......nnnnn.DDDD -- SVE pointer conflict compare
            emitDispPredicateReg(id->idReg1(), insGetPredicateType(fmt), id->idInsOpt(), true); // DDDD
            emitDispReg(id->idReg2(), id->idOpSize(), true);                                    // nnnnn
            emitDispReg(id->idReg3(), id->idOpSize(), false);                                   // mmmmm
            break;

        // <Pd>, <Pn>, <Pm>.<T>[<Wv>, <imm>]
        case IF_SVE_DV_4A: // ........ix.xxxvv ..NNNN.MMMM.DDDD -- SVE broadcast predicate element
            emitDispPredicateReg(id->idReg1(), insGetPredicateType(fmt, 1), id->idInsOpt(), true);  // DDDD
            emitDispPredicateReg(id->idReg2(), insGetPredicateType(fmt, 2), id->idInsOpt(), true);  // NNNN
            emitDispPredicateReg(id->idReg3(), insGetPredicateType(fmt, 3), id->idInsOpt(), false); // MMMM
            printf("[");
            emitDispReg(id->idReg4(), EA_4BYTE, true); // vv
            emitDispImm(emitGetInsSC(id), false);      // ix xx
            printf("]");
            break;

        // <Pd>.<T>, <PNn>[<imm>]
        case IF_SVE_DW_2A: // ........xx...... ......iiNNN.DDDD -- SVE extract mask predicate from predicate-as-counter
            emitDispPredicateReg(id->idReg1(), PREDICATE_SIZED, id->idInsOpt(), true); // DDDD
            emitDispPredicateReg(id->idReg2(), PREDICATE_N, id->idInsOpt(), false);    // NNN
            emitDispElementIndex(emitGetInsSC(id), false);                             // ii
            break;

        // {<Pd1>.<T>, <Pd2>.<T>}, <PNn>[<imm>]
        case IF_SVE_DW_2B: // ........xx...... .......iNNN.DDDD -- SVE extract mask predicate from predicate-as-counter
            emitDispPredicateRegPair(id->idReg1(), id->idInsOpt());                 // DDDD
            emitDispPredicateReg(id->idReg2(), PREDICATE_N, id->idInsOpt(), false); // NNN
            emitDispElementIndex(emitGetInsSC(id), false);                          // i
            break;

        // {<Pd1>.<T>, <Pd2>.<T>}, <Xn>, <Xm>
        case IF_SVE_DX_3A: // ........xx.mmmmm ......nnnnn.DDD. -- SVE integer compare scalar count and limit (predicate
                           // pair)
            emitDispLowPredicateRegPair(id->idReg1(), id->idInsOpt());
            emitDispReg(id->idReg2(), id->idOpSize(), true);  // nnnnn
            emitDispReg(id->idReg3(), id->idOpSize(), false); // mmmmm
            break;

        // <PNd>.<T>, <Xn>, <Xm>, <vl>
        case IF_SVE_DY_3A: // ........xx.mmmmm ..l...nnnnn..DDD -- SVE integer compare scalar count and limit
                           // (predicate-as-counter)
            emitDispPredicateReg(id->idReg1(), insGetPredicateType(fmt), id->idInsOpt(), true); // DDD
            emitDispReg(id->idReg2(), id->idOpSize(), true);                                    // nnnnn
            emitDispReg(id->idReg3(), id->idOpSize(), true);                                    // mmmmm
            emitDispVectorLengthSpecifier(id);
            break;

        // PTRUE <PNd>.<T>
        case IF_SVE_DZ_1A: // ........xx...... .............DDD -- sve_int_pn_ptrue
            emitDispPredicateReg(id->idReg1(), insGetPredicateType(fmt), id->idInsOpt(), false); // DDD
            break;

        // FDUP <Zd>.<T>, #<const>
        // FMOV <Zd>.<T>, #<const>
        case IF_SVE_EA_1A: // ........xx...... ...iiiiiiiiddddd -- SVE broadcast floating-point immediate (unpredicated)
            emitDispSveReg(id->idReg1(), id->idInsOpt(), true); // ddddd
            emitDispFloatImm(emitGetInsSC(id));                 // iiiiiiii
            break;

        // DUP <Zd>.<T>, #<imm>{, <shift>}
        // MOV <Zd>.<T>, #<imm>{, <shift>}
        case IF_SVE_EB_1A: // ........xx...... ..hiiiiiiiiddddd -- SVE broadcast integer immediate (unpredicated)
        {
            imm = emitGetInsSC(id);
            emitDispSveReg(id->idReg1(), id->idInsOpt(), true); // ddddd
            emitDispImmOptsLSL(imm, id->idHasShift(), 8);       // h iiiiiiii
            break;
        }

        // ADD <Zdn>.<T>, <Zdn>.<T>, #<imm>{, <shift>}
        // SQADD <Zdn>.<T>, <Zdn>.<T>, #<imm>{, <shift>}
        // UQADD <Zdn>.<T>, <Zdn>.<T>, #<imm>{, <shift>}
        // SUB <Zdn>.<T>, <Zdn>.<T>, #<imm>{, <shift>}
        // SUBR <Zdn>.<T>, <Zdn>.<T>, #<imm>{, <shift>}
        // SQSUB <Zdn>.<T>, <Zdn>.<T>, #<imm>{, <shift>}
        // UQSUB <Zdn>.<T>, <Zdn>.<T>, #<imm>{, <shift>}
        case IF_SVE_EC_1A: // ........xx...... ..hiiiiiiiiddddd -- SVE integer add/subtract immediate (unpredicated)
        {
            imm = emitGetInsSC(id);
            emitDispSveReg(id->idReg1(), id->idInsOpt(), true); // ddddd
            emitDispSveReg(id->idReg1(), id->idInsOpt(), true); // ddddd
            emitDispImmOptsLSL(imm, id->idHasShift(), 8);       // h iiiiiiii
            break;
        }

        // FMOV <Zd>.<T>, #0.0
        // (Preferred disassembly: FMOV <Zd>.<T>, #0)
        case IF_SVE_EB_1B: // ........xx...... ...........ddddd -- SVE broadcast integer immediate (unpredicated)
            emitDispSveReg(id->idReg1(), id->idInsOpt(), true); // ddddd
            emitDispImm(0, false);
            break;

        // SMAX <Zdn>.<T>, <Zdn>.<T>, #<imm>
        // SMIN <Zdn>.<T>, <Zdn>.<T>, #<imm>
        // UMAX <Zdn>.<T>, <Zdn>.<T>, #<imm>
        // UMIN <Zdn>.<T>, <Zdn>.<T>, #<imm>
        case IF_SVE_ED_1A: // ........xx...... ...iiiiiiiiddddd -- SVE integer min/max immediate (unpredicated)
        // MUL <Zdn>.<T>, <Zdn>.<T>, #<imm>
        case IF_SVE_EE_1A: // ........xx...... ...iiiiiiiiddddd -- SVE integer multiply immediate (unpredicated)
            emitDispSveReg(id->idReg1(), id->idInsOpt(), true); // ddddd
            emitDispSveReg(id->idReg1(), id->idInsOpt(), true); // ddddd
            emitDispImm(emitGetInsSC(id), false);               // iiiiiiii
            break;

        // <Zda>.<T>, <Zn>.<Tb>, <Zm>.<Tb>
        case IF_SVE_EH_3A: // ........xx.mmmmm ......nnnnnddddd -- SVE integer dot product (unpredicated)
        // <Zda>.S, <Zn>.B, <Zm>.B
        case IF_SVE_FO_3A: // ...........mmmmm ......nnnnnddddd -- SVE integer matrix multiply accumulate
        {
            const insOpts smallSizeSpecifier = (insOpts)(id->idInsOpt() - 2);
            emitDispSveReg(id->idReg1(), id->idInsOpt(), true);      // ddddd
            emitDispSveReg(id->idReg2(), smallSizeSpecifier, true);  // nnnnn
            emitDispSveReg(id->idReg3(), smallSizeSpecifier, false); // mmmmm
            break;
        }

        // <Zda>.<T>, <Zn>.<Tb>, <Zm>.<Tb>
        case IF_SVE_EL_3A: // ........xx.mmmmm ......nnnnnddddd
        // <Zd>.<T>, <Zn>.<Tb>, <Zm>.<Tb>
        case IF_SVE_FL_3A: // ........xx.mmmmm ......nnnnnddddd
        // <Zd>.Q, <Zn>.D, <Zm>.D
        case IF_SVE_FN_3B: // ...........mmmmm ......nnnnnddddd -- SVE2 integer multiply long
        {
            const insOpts smallSizeSpecifier = (insOpts)(id->idInsOpt() - 1);
            emitDispSveReg(id->idReg1(), id->idInsOpt(), true);      // ddddd
            emitDispSveReg(id->idReg2(), smallSizeSpecifier, true);  // nnnnn
            emitDispSveReg(id->idReg3(), smallSizeSpecifier, false); // mmmmm
            break;
        }

        // <Zd>.<T>, <Zn>.<Tb>, <Zm>.<Tb>
        case IF_SVE_GC_3A: // ........xx.mmmmm ......nnnnnddddd -- SVE2 integer add/subtract narrow high part
        {
            const insOpts largeSizeSpecifier = (insOpts)(id->idInsOpt() + 1);
            emitDispSveReg(id->idReg1(), id->idInsOpt(), true);      // ddddd
            emitDispSveReg(id->idReg2(), largeSizeSpecifier, true);  // nnnnn
            emitDispSveReg(id->idReg3(), largeSizeSpecifier, false); // mmmmm
            break;
        }

        // <Zd>.<T>, <Zn>.<T>, <Zm>.<Tb>
        case IF_SVE_FM_3A: // ........xx.mmmmm ......nnnnnddddd -- SVE2 integer add/subtract wide
        {
            const insOpts smallSizeSpecifier = (insOpts)(id->idInsOpt() - 1);
            emitDispSveReg(id->idReg1(), id->idInsOpt(), true);      // ddddd
            emitDispSveReg(id->idReg2(), id->idInsOpt(), true);      // nnnnn
            emitDispSveReg(id->idReg3(), smallSizeSpecifier, false); // mmmmm
            break;
        }

        // CDOT <Zda>.<T>, <Zn>.<Tb>, <Zm>.<Tb>, <const>
        case IF_SVE_EJ_3A: // ........xx.mmmmm ....rrnnnnnddddd -- SVE2 complex integer dot product
        {
            const insOpts smallSizeSpecifier = (insOpts)(id->idInsOpt() - 2);
            emitDispSveReg(id->idReg1(), id->idInsOpt(), true);     // ddddd
            emitDispSveReg(id->idReg2(), smallSizeSpecifier, true); // nnnnn
            emitDispSveReg(id->idReg3(), smallSizeSpecifier, true); // mmmmm

            // rot specifies a multiple of 90-degree rotations
            emitDispImm(emitDecodeRotationImm0_to_270(emitGetInsSC(id)), false); // rr
            break;
        }

        // CMLA <Zda>.<T>, <Zn>.<T>, <Zm>.<T>, <const>
        // SQRDCMLAH <Zda>.<T>, <Zn>.<T>, <Zm>.<T>, <const>
        case IF_SVE_EK_3A: // ........xx.mmmmm ....rrnnnnnddddd -- SVE2 complex integer multiply-add
            emitDispSveReg(id->idReg1(), id->idInsOpt(), true); // ddddd
            emitDispSveReg(id->idReg2(), id->idInsOpt(), true); // nnnnn
            emitDispSveReg(id->idReg3(), id->idInsOpt(), true); // mmmmm

            // rot specifies a multiple of 90-degree rotations
            emitDispImm(emitDecodeRotationImm0_to_270(emitGetInsSC(id)), false); // rr
            break;

        // CDOT <Zda>.S, <Zn>.B, <Zm>.B[<imm>], <const>
        case IF_SVE_FA_3A: // ...........iimmm ....rrnnnnnddddd -- SVE2 complex integer dot product (indexed)
        {
            const ssize_t imm   = emitGetInsSC(id);
            const ssize_t rot   = (imm & 0b11);
            const ssize_t index = (imm >> 2);
            emitDispSveReg(id->idReg1(), INS_OPTS_SCALABLE_S, true); // ddddd
            emitDispSveReg(id->idReg2(), id->idInsOpt(), true);      // nnnnn
            emitDispSveReg(id->idReg3(), id->idInsOpt(), false);     // mmm
            emitDispElementIndex(index, true);                       // ii

            // rot specifies a multiple of 90-degree rotations
            emitDispImm(emitDecodeRotationImm0_to_270(rot), false); // rr
            break;
        }

        // CDOT <Zda>.D, <Zn>.H, <Zm>.H[<imm>], <const>
        case IF_SVE_FA_3B: // ...........immmm ....rrnnnnnddddd -- SVE2 complex integer dot product (indexed)
        {
            const ssize_t imm   = emitGetInsSC(id);
            const ssize_t rot   = (imm & 0b11);
            const ssize_t index = (imm >> 2);
            emitDispSveReg(id->idReg1(), INS_OPTS_SCALABLE_D, true); // ddddd
            emitDispSveReg(id->idReg2(), id->idInsOpt(), true);      // nnnnn
            emitDispSveReg(id->idReg3(), id->idInsOpt(), false);     // mmm
            emitDispElementIndex(index, true);                       // i

            // rot specifies a multiple of 90-degree rotations
            emitDispImm(emitDecodeRotationImm0_to_270(rot), false); // rr
            break;
        }

        // CMLA <Zda>.H, <Zn>.H, <Zm>.H[<imm>], <const>
        case IF_SVE_FB_3A: // ...........iimmm ....rrnnnnnddddd -- SVE2 complex integer multiply-add (indexed)
        // CMLA <Zda>.S, <Zn>.S, <Zm>.S[<imm>], <const>
        case IF_SVE_FB_3B: // ...........immmm ....rrnnnnnddddd -- SVE2 complex integer multiply-add (indexed)
        // SQRDCMLAH <Zda>.H, <Zn>.H, <Zm>.H[<imm>], <const>
        case IF_SVE_FC_3A: // ...........iimmm ....rrnnnnnddddd -- SVE2 complex saturating multiply-add (indexed)
        // SQRDCMLAH <Zda>.S, <Zn>.S, <Zm>.S[<imm>], <const>
        case IF_SVE_FC_3B: // ...........immmm ....rrnnnnnddddd -- SVE2 complex saturating multiply-add (indexed)
        // FCMLA <Zda>.S, <Zn>.S, <Zm>.S[<imm>], <const>
        case IF_SVE_GV_3A: // ...........immmm ....rrnnnnnddddd -- SVE floating-point complex multiply-add (indexed)
        {
            const ssize_t imm   = emitGetInsSC(id);
            const ssize_t rot   = (imm & 0b11);
            const ssize_t index = (imm >> 2);
            emitDispSveReg(id->idReg1(), id->idInsOpt(), true);  // ddddd
            emitDispSveReg(id->idReg2(), id->idInsOpt(), true);  // nnnnn
            emitDispSveReg(id->idReg3(), id->idInsOpt(), false); // mmm
            emitDispElementIndex(index, true);                   // i

            // rot specifies a multiple of 90-degree rotations
            emitDispImm(emitDecodeRotationImm0_to_270(rot), false); // rr
            break;
        }

        // <Zd>.H, <Pg>/M, <Zn>.S
        // <Zd>.S, <Pg>/M, <Zn>.D
        // <Zd>.D, <Pg>/M, <Zn>.S
        // <Zd>.S, <Pg>/M, <Zn>.H
        // <Zd>.D, <Pg>/M, <Zn>.D
        // <Zd>.S, <Pg>/M, <Zn>.S
        // <Zd>.D, <Pg>/M, <Zn>.H
        // <Zd>.H, <Pg>/M, <Zn>.H
        // <Zd>.H, <Pg>/M, <Zn>.D
        // <Zd>.H, <Pg>/M, <Zn>.S
        case IF_SVE_GQ_3A: // ................ ...gggnnnnnddddd -- SVE floating-point convert precision odd elements
        case IF_SVE_HO_3A: // ................ ...gggnnnnnddddd -- SVE floating-point convert precision
        case IF_SVE_HO_3B: // ................ ...gggnnnnnddddd -- SVE floating-point convert precision
        case IF_SVE_HO_3C: // ................ ...gggnnnnnddddd -- SVE floating-point convert precision
        case IF_SVE_HP_3B: // ................ ...gggnnnnnddddd -- SVE floating-point convert to integer
        case IF_SVE_HS_3A: // ................ ...gggnnnnnddddd -- SVE integer convert to floating-point
        {
            insOpts opt = id->idInsOpt();

            switch (ins)
            {
                // These cases have only one combination of operands so the option may be omitted.
                case INS_sve_fcvtxnt:
                    opt = INS_OPTS_D_TO_S;
                    break;
                case INS_sve_bfcvtnt:
                    opt = INS_OPTS_S_TO_H;
                    break;
                case INS_sve_fcvtx:
                    opt = INS_OPTS_D_TO_S;
                    break;
                case INS_sve_bfcvt:
                    opt = INS_OPTS_S_TO_H;
                    break;
                default:
                    break;
            }

            insOpts dst = INS_OPTS_NONE;
            insOpts src = INS_OPTS_NONE;
            optExpandConversionPair(opt, dst, src);

            emitDispSveReg(id->idReg1(), dst, true);                                            // ddddd
            emitDispPredicateReg(id->idReg2(), insGetPredicateType(fmt), id->idInsOpt(), true); // ggg
            emitDispSveReg(id->idReg3(), src, false);                                           // nnnnn
            break;
        }

        // { <Zt>.D }, <Pg>/Z, [<Xn|SP>{, #<imm>, MUL VL}]
        // Some of these formats may allow changing the element size instead of using 'D' for all instructions.
        case IF_SVE_IH_3A:   // ............iiii ...gggnnnnnttttt -- SVE contiguous load (quadwords, scalar plus
                             // immediate)
        case IF_SVE_IH_3A_A: // ............iiii ...gggnnnnnttttt -- SVE contiguous load (quadwords, scalar plus
                             // immediate)
        case IF_SVE_IH_3A_F: // ............iiii ...gggnnnnnttttt -- SVE contiguous load (quadwords, scalar plus
                             // immediate)
        case IF_SVE_IJ_3A:   // ............iiii ...gggnnnnnttttt -- SVE contiguous load (scalar plus immediate)
        case IF_SVE_IJ_3A_D: // ............iiii ...gggnnnnnttttt -- SVE contiguous load (scalar plus immediate)
        case IF_SVE_IJ_3A_E: // ............iiii ...gggnnnnnttttt -- SVE contiguous load (scalar plus immediate)
        case IF_SVE_IJ_3A_F: // ............iiii ...gggnnnnnttttt -- SVE contiguous load (scalar plus immediate)
        case IF_SVE_IJ_3A_G: // ............iiii ...gggnnnnnttttt -- SVE contiguous load (scalar plus immediate)
        case IF_SVE_IL_3A: // ............iiii ...gggnnnnnttttt -- SVE contiguous non-fault load (scalar plus immediate)
        case IF_SVE_IL_3A_A: // ............iiii ...gggnnnnnttttt -- SVE contiguous non-fault load (scalar plus
                             // immediate)
        case IF_SVE_IL_3A_B: // ............iiii ...gggnnnnnttttt -- SVE contiguous non-fault load (scalar plus
                             // immediate)
        case IF_SVE_IL_3A_C: // ............iiii ...gggnnnnnttttt -- SVE contiguous non-fault load (scalar plus
                             // immediate)
        case IF_SVE_IM_3A:   // ............iiii ...gggnnnnnttttt -- SVE contiguous non-temporal load (scalar plus
                             // immediate)
        // { <Zt>.B }, <Pg>/Z, [<Xn|SP>{, #<imm>}]
        // { <Zt>.H }, <Pg>/Z, [<Xn|SP>{, #<imm>}]
        // { <Zt>.S }, <Pg>/Z, [<Xn|SP>{, #<imm>}]
        // { <Zt>.D }, <Pg>/Z, [<Xn|SP>{, #<imm>}]
        case IF_SVE_IO_3A: // ............iiii ...gggnnnnnttttt -- SVE load and broadcast quadword (scalar plus
                           // immediate)
        // { <Zt1>.Q, <Zt2>.Q }, <Pg>/Z, [<Xn|SP>{, #<imm>, MUL VL}]
        // { <Zt1>.Q, <Zt2>.Q, <Zt3>.Q }, <Pg>/Z, [<Xn|SP>{, #<imm>, MUL VL}]
        // { <Zt1>.Q, <Zt2>.Q, <Zt3>.Q, <Zt4>.Q }, <Pg>/Z, [<Xn|SP>{, #<imm>, MUL VL}]
        case IF_SVE_IQ_3A: // ............iiii ...gggnnnnnttttt -- SVE load multiple structures (quadwords, scalar plus
                           // immediate)
        // { <Zt1>.B, <Zt2>.B }, <Pg>/Z, [<Xn|SP>{, #<imm>, MUL VL}]
        // { <Zt1>.H, <Zt2>.H }, <Pg>/Z, [<Xn|SP>{, #<imm>, MUL VL}]
        // { <Zt1>.S, <Zt2>.S }, <Pg>/Z, [<Xn|SP>{, #<imm>, MUL VL}]
        // { <Zt1>.D, <Zt2>.D }, <Pg>/Z, [<Xn|SP>{, #<imm>, MUL VL}]
        // { <Zt1>.B, <Zt2>.B, <Zt3>.B }, <Pg>/Z, [<Xn|SP>{, #<imm>, MUL VL}]
        // { <Zt1>.H, <Zt2>.H, <Zt3>.H }, <Pg>/Z, [<Xn|SP>{, #<imm>, MUL VL}]
        // { <Zt1>.S, <Zt2>.S, <Zt3>.S }, <Pg>/Z, [<Xn|SP>{, #<imm>, MUL VL}]
        // { <Zt1>.D, <Zt2>.D, <Zt3>.D }, <Pg>/Z, [<Xn|SP>{, #<imm>, MUL VL}]
        // { <Zt1>.B, <Zt2>.B, <Zt3>.B, <Zt4>.B }, <Pg>/Z, [<Xn|SP>{, #<imm>, MUL VL}]
        // { <Zt1>.H, <Zt2>.H, <Zt3>.H, <Zt4>.H }, <Pg>/Z, [<Xn|SP>{, #<imm>, MUL VL}]
        // { <Zt1>.S, <Zt2>.S, <Zt3>.S, <Zt4>.S }, <Pg>/Z, [<Xn|SP>{, #<imm>, MUL VL}]
        // { <Zt1>.D, <Zt2>.D, <Zt3>.D, <Zt4>.D }, <Pg>/Z, [<Xn|SP>{, #<imm>, MUL VL}]
        case IF_SVE_IS_3A: // ............iiii ...gggnnnnnttttt -- SVE load multiple structures (scalar plus immediate)
        // { <Zt1>.Q, <Zt2>.Q }, <Pg>, [<Xn|SP>{, #<imm>, MUL VL}]
        // { <Zt1>.Q, <Zt2>.Q, <Zt3>.Q }, <Pg>, [<Xn|SP>{, #<imm>, MUL VL}]
        // { <Zt1>.Q, <Zt2>.Q, <Zt3>.Q, <Zt4>.Q }, <Pg>, [<Xn|SP>{, #<imm>, MUL VL}]
        case IF_SVE_JE_3A: // ............iiii ...gggnnnnnttttt -- SVE store multiple structures (quadwords, scalar plus
                           // immediate)
        // { <Zt>.B }, <Pg>, [<Xn|SP>{, #<imm>, MUL VL}]
        // { <Zt>.H }, <Pg>, [<Xn|SP>{, #<imm>, MUL VL}]
        // { <Zt>.S }, <Pg>, [<Xn|SP>{, #<imm>, MUL VL}]
        // { <Zt>.D }, <Pg>, [<Xn|SP>{, #<imm>, MUL VL}]
        case IF_SVE_JM_3A: // ............iiii ...gggnnnnnttttt -- SVE contiguous non-temporal store (scalar plus
                           // immediate)
        // { <Zt>.D }, <Pg>, [<Xn|SP>{, #<imm>, MUL VL}]
        // { <Zt>.Q }, <Pg>, [<Xn|SP>{, #<imm>, MUL VL}]
        case IF_SVE_JN_3C:   // ............iiii ...gggnnnnnttttt -- SVE contiguous store (scalar plus immediate)
        case IF_SVE_JN_3C_D: // ............iiii ...gggnnnnnttttt -- SVE contiguous store (scalar plus immediate)
        // { <Zt1>.B, <Zt2>.B }, <Pg>, [<Xn|SP>{, #<imm>, MUL VL}]
        // { <Zt1>.H, <Zt2>.H }, <Pg>, [<Xn|SP>{, #<imm>, MUL VL}]
        // { <Zt1>.S, <Zt2>.S }, <Pg>, [<Xn|SP>{, #<imm>, MUL VL}]
        // { <Zt1>.D, <Zt2>.D }, <Pg>, [<Xn|SP>{, #<imm>, MUL VL}]
        // { <Zt1>.B, <Zt2>.B, <Zt3>.B }, <Pg>, [<Xn|SP>{, #<imm>, MUL VL}]
        // { <Zt1>.H, <Zt2>.H, <Zt3>.H }, <Pg>, [<Xn|SP>{, #<imm>, MUL VL}]
        // { <Zt1>.S, <Zt2>.S, <Zt3>.S }, <Pg>, [<Xn|SP>{, #<imm>, MUL VL}]
        // { <Zt1>.D, <Zt2>.D, <Zt3>.D }, <Pg>, [<Xn|SP>{, #<imm>, MUL VL}]
        // { <Zt1>.B, <Zt2>.B, <Zt3>.B, <Zt4>.B }, <Pg>, [<Xn|SP>{, #<imm>, MUL VL}]
        // { <Zt1>.H, <Zt2>.H, <Zt3>.H, <Zt4>.H }, <Pg>, [<Xn|SP>{, #<imm>, MUL VL}]
        // { <Zt1>.S, <Zt2>.S, <Zt3>.S, <Zt4>.S }, <Pg>, [<Xn|SP>{, #<imm>, MUL VL}]
        // { <Zt1>.D, <Zt2>.D, <Zt3>.D, <Zt4>.D }, <Pg>, [<Xn|SP>{, #<imm>, MUL VL}]
        case IF_SVE_JO_3A: // ............iiii ...gggnnnnnttttt -- SVE store multiple structures (scalar plus immediate)
            imm = emitGetInsSC(id);
            emitDispSveConsecutiveRegList(id->idReg1(), insGetSveReg1ListSize(ins), id->idInsOpt(), true); // ttttt
            emitDispPredicateReg(id->idReg2(), insGetPredicateType(fmt), id->idInsOpt(), true);            // ggg
            printf("[");
            emitDispReg(id->idReg3(), EA_8BYTE, imm != 0); // nnnnn
            if (imm != 0)
            {
                switch (fmt)
                {
                    case IF_SVE_IO_3A:
                        // This does not have to be printed as hex.
                        // We only do it because the capstone disassembly displays this immediate as hex.
                        // We could not modify capstone without affecting other cases.
                        emitDispImm(emitGetInsSC(id), false, /* alwaysHex */ true); // iiii
                        break;

                    case IF_SVE_IQ_3A:
                    case IF_SVE_IS_3A:
                    case IF_SVE_JE_3A:
                    case IF_SVE_JO_3A:
                        // This does not have to be printed as hex.
                        // We only do it because the capstone disassembly displays this immediate as hex.
                        // We could not modify capstone without affecting other cases.
                        emitDispImm(emitGetInsSC(id), true, /* alwaysHex */ true); // iiii
                        printf("mul vl");
                        break;

                    default:
                        emitDispImm(emitGetInsSC(id), true); // iiii
                        printf("mul vl");
                        break;
                }
            }
            printf("]");
            break;

        // {<Zt>.<T>}, <Pg>, [<Xn|SP>, <Xm>]
        // {<Zt>.<T>}, <Pg>, [<Xn|SP>, <Xm>, LSL #1]
        case IF_SVE_JD_4A: // .........xxmmmmm ...gggnnnnnttttt -- SVE contiguous store (scalar plus scalar)
        // {<Zt>.<T>}, <Pg>, [<Xn|SP>, <Xm>, LSL #2]
        case IF_SVE_JD_4B: // ..........xmmmmm ...gggnnnnnttttt -- SVE contiguous store (scalar plus scalar)
        // {<Zt>.D }, <Pg>, [<Xn|SP>, <Zm>.D, <mod> #3]
        // {<Zt>.S }, <Pg>, [<Xn|SP>, <Zm>.S, <mod> #1]
        // {<Zt>.S }, <Pg>, [<Xn|SP>, <Zm>.S, <mod> #2]
        case IF_SVE_JJ_4A: // ...........mmmmm .h.gggnnnnnttttt -- SVE 64-bit scatter store (scalar plus 64-bit scaled
                           // offsets)
        // {<Zt>.D }, <Pg>, [<Xn|SP>, <Zm>.D, <mod>]
        // {<Zt>.D }, <Pg>, [<Xn|SP>, <Zm>.D, <mod> #1]
        // {<Zt>.D }, <Pg>, [<Xn|SP>, <Zm>.D, <mod> #2]
        case IF_SVE_JJ_4A_B: // ...........mmmmm .h.gggnnnnnttttt -- SVE 64-bit scatter store (scalar plus 64-bit scaled
                             // offsets)
        // {<Zt>.D }, <Pg>, [<Xn|SP>, <Zm>.D, <mod>]
        case IF_SVE_JJ_4A_C: // ...........mmmmm .h.gggnnnnnttttt -- SVE 64-bit scatter store (scalar plus 64-bit scaled
                             // offsets)
        // {<Zt>.S }, <Pg>, [<Xn|SP>, <Zm>.S, <mod>]
        case IF_SVE_JJ_4A_D: // ...........mmmmm .h.gggnnnnnttttt -- SVE 64-bit scatter store (scalar plus 64-bit scaled
                             // offsets)
        // {<Zt>.D }, <Pg>, [<Xn|SP>, <Zm>.D, <mod>]
        case IF_SVE_JK_4A: // ...........mmmmm .h.gggnnnnnttttt -- SVE 64-bit scatter store (scalar plus 64-bit unscaled
                           // offsets)
        // {<Zt>.S }, <Pg>, [<Xn|SP>, <Zm>.S, <mod>]
        case IF_SVE_JK_4A_B: // ...........mmmmm .h.gggnnnnnttttt -- SVE 64-bit scatter store (scalar plus 64-bit
                             // unscaled offsets)
        // {<Zt>.D }, <Pg>/Z, [<Xn|SP>, <Zm>.D, <mod>]
        // {<Zt>.S }, <Pg>/Z, [<Xn|SP>, <Zm>.S, <mod> #1]
        // {<Zt>.S }, <Pg>/Z, [<Xn|SP>, <Zm>.S, <mod> #2]
        case IF_SVE_HW_4A: // .........h.mmmmm ...gggnnnnnttttt -- SVE 32-bit gather load (scalar plus 32-bit unscaled
                           // offsets)
        // {<Zt>.S }, <Pg>/Z, [<Xn|SP>, <Zm>.S, <mod>]
        // {<Zt>.D }, <Pg>/Z, [<Xn|SP>, <Zm>.D, <mod> #1]
        // {<Zt>.D }, <Pg>/Z, [<Xn|SP>, <Zm>.D, <mod> #2]
        case IF_SVE_HW_4A_A: // .........h.mmmmm ...gggnnnnnttttt -- SVE 32-bit gather load (scalar plus 32-bit unscaled
                             // offsets)
        // {<Zt>.D }, <Pg>/Z, [<Xn|SP>, <Zm>.D, <mod>]
        case IF_SVE_HW_4A_B: // .........h.mmmmm ...gggnnnnnttttt -- SVE 32-bit gather load (scalar plus 32-bit unscaled
                             // offsets)
        // {<Zt>.S }, <Pg>/Z, [<Xn|SP>, <Zm>.S, <mod>]
        case IF_SVE_HW_4A_C: // .........h.mmmmm ...gggnnnnnttttt -- SVE 32-bit gather load (scalar plus 32-bit unscaled
                             // offsets)
        // {<Zt>.D }, <Pg>/Z, [<Xn|SP>, <Zm>.D, <mod> #2]
        // {<Zt>.D }, <Pg>/Z, [<Xn|SP>, <Zm>.D, <mod> #3]
        case IF_SVE_IU_4A: // .........h.mmmmm ...gggnnnnnttttt -- SVE 64-bit gather load (scalar plus 32-bit unpacked
                           // scaled offsets)
        // {<Zt>.D }, <Pg>/Z, [<Xn|SP>, <Zm>.D, <mod>]
        case IF_SVE_IU_4A_A: // .........h.mmmmm ...gggnnnnnttttt -- SVE 64-bit gather load (scalar plus 32-bit unpacked
                             // scaled offsets)
        // {<Zt>.D }, <Pg>/Z, [<Xn|SP>, <Zm>.D, <mod>]
        case IF_SVE_IU_4A_C: // .........h.mmmmm ...gggnnnnnttttt -- SVE 64-bit gather load (scalar plus 32-bit unpacked
                             // scaled offsets)
        // {<Zt>.D }, <Pg>/Z, [<Xn|SP>, <Zm>.D]
        // {<Zt>.D }, <Pg>/Z, [<Xn|SP>, <Zm>.D, LSL #1]
        // {<Zt>.D }, <Pg>/Z, [<Xn|SP>, <Zm>.D, LSL #2]
        case IF_SVE_HW_4B: // ...........mmmmm ...gggnnnnnttttt -- SVE 32-bit gather load (scalar plus 32-bit unscaled
                           // offsets)
        // {<Zt>.D }, <Pg>/Z, [<Xn|SP>, <Zm>.D]
        case IF_SVE_HW_4B_D: // ...........mmmmm ...gggnnnnnttttt -- SVE 32-bit gather load (scalar plus 32-bit unscaled
                             // offsets)
        // {<Zt>.S }, <Pg>/Z, [<Zn>.S{, <Xm>}]
        case IF_SVE_IF_4A: // ...........mmmmm ...gggnnnnnttttt -- SVE2 32-bit gather non-temporal load (vector plus
                           // scalar)
        // {<Zt>.D }, <Pg>/Z, [<Zn>.D{, <Xm>}]
        case IF_SVE_IF_4A_A: // ...........mmmmm ...gggnnnnnttttt -- SVE2 32-bit gather non-temporal load (vector plus
                             // scalar)
        // {<Zt>.D }, <Pg>/Z, [<Xn|SP>{, <Xm>, LSL #3}]
        // {<Zt>.D }, <Pg>/Z, [<Xn|SP>{, <Xm>, LSL #2}]
        case IF_SVE_IG_4A: // ...........mmmmm ...gggnnnnnttttt -- SVE contiguous first-fault load (scalar plus scalar)
        // {<Zt>.H }, <Pg>/Z, [<Xn|SP>{, <Xm>}]
        // {<Zt>.S }, <Pg>/Z, [<Xn|SP>{, <Xm>}]
        // {<Zt>.D }, <Pg>/Z, [<Xn|SP>{, <Xm>}]
        case IF_SVE_IG_4A_D: // ...........mmmmm ...gggnnnnnttttt -- SVE contiguous first-fault load (scalar plus
                             // scalar)
        // {<Zt>.B }, <Pg>/Z, [<Xn|SP>{, <Xm>}]
        // {<Zt>.H }, <Pg>/Z, [<Xn|SP>{, <Xm>}]
        // {<Zt>.S }, <Pg>/Z, [<Xn|SP>{, <Xm>}]
        // {<Zt>.D }, <Pg>/Z, [<Xn|SP>{, <Xm>}]
        case IF_SVE_IG_4A_E: // ...........mmmmm ...gggnnnnnttttt -- SVE contiguous first-fault load (scalar plus
                             // scalar)
        // {<Zt>.S }, <Pg>/Z, [<Xn|SP>{, <Xm>, LSL #1}]
        // {<Zt>.D }, <Pg>/Z, [<Xn|SP>{, <Xm>, LSL #1}]
        // {<Zt>.S }, <Pg>/Z, [<Xn|SP>{, <Xm>, LSL #2}]
        // {<Zt>.D }, <Pg>/Z, [<Xn|SP>{, <Xm>, LSL #2}]
        case IF_SVE_IG_4A_F: // ...........mmmmm ...gggnnnnnttttt -- SVE contiguous first-fault load (scalar plus
                             // scalar)
        // {<Zt>.H }, <Pg>/Z, [<Xn|SP>{, <Xm>, LSL #1}]
        // {<Zt>.S }, <Pg>/Z, [<Xn|SP>{, <Xm>, LSL #1}]
        // {<Zt>.D }, <Pg>/Z, [<Xn|SP>{, <Xm>, LSL #1}]
        case IF_SVE_IG_4A_G: // ...........mmmmm ...gggnnnnnttttt -- SVE contiguous first-fault load (scalar plus
                             // scalar)
        // {<Zt>.D }, <Pg>/Z, [<Xn|SP>, <Xm>, LSL #3]
        case IF_SVE_II_4A: // ...........mmmmm ...gggnnnnnttttt -- SVE contiguous load (quadwords, scalar plus scalar)
        // {<Zt>.Q }, <Pg>/Z, [<Xn|SP>, <Xm>, LSL #3]
        case IF_SVE_II_4A_B: // ...........mmmmm ...gggnnnnnttttt -- SVE contiguous load (quadwords, scalar plus scalar)
        // {<Zt>.D }, <Pg>/Z, [<Xn|SP>, <Xm>, LSL #2]
        case IF_SVE_II_4A_H: // ...........mmmmm ...gggnnnnnttttt -- SVE contiguous load (quadwords, scalar plus scalar)
        // {<Zt>.D }, <Pg>/Z, [<Xn|SP>, <Xm>, LSL #2
        case IF_SVE_IK_4A: // ...........mmmmm ...gggnnnnnttttt -- SVE contiguous load (scalar plus scalar)
        // {<Zt>.H }, <Pg>/Z, [<Xn|SP>, <Xm>]
        // {<Zt>.S }, <Pg>/Z, [<Xn|SP>, <Xm>]
        // {<Zt>.D }, <Pg>/Z, [<Xn|SP>, <Xm>]
        case IF_SVE_IK_4A_F: // ...........mmmmm ...gggnnnnnttttt -- SVE contiguous load (scalar plus scalar)
        // {<Zt>.S }, <Pg>/Z, [<Xn|SP>, <Xm>, LSL #1]
        // {<Zt>.D }, <Pg>/Z, [<Xn|SP>, <Xm>, LSL #1]
        case IF_SVE_IK_4A_G: // ...........mmmmm ...gggnnnnnttttt -- SVE contiguous load (scalar plus scalar)
        // {<Zt>.B }, <Pg>/Z, [<Xn|SP>, <Xm>]
        // {<Zt>.H }, <Pg>/Z, [<Xn|SP>, <Xm>]
        // {<Zt>.S }, <Pg>/Z, [<Xn|SP>, <Xm>]
        // {<Zt>.D }, <Pg>/Z, [<Xn|SP>, <Xm>]
        case IF_SVE_IK_4A_H: // ...........mmmmm ...gggnnnnnttttt -- SVE contiguous load (scalar plus scalar)
        // {<Zt>.H }, <Pg>/Z, [<Xn|SP>, <Xm>, LSL #1]
        // {<Zt>.S }, <Pg>/Z, [<Xn|SP>, <Xm>, LSL #1]
        // {<Zt>.D }, <Pg>/Z, [<Xn|SP>, <Xm>, LSL #1]
        case IF_SVE_IK_4A_I: // ...........mmmmm ...gggnnnnnttttt -- SVE contiguous load (scalar plus scalar)
        // {<Zt>.B }, <Pg>/Z, [<Xn|SP>, <Xm>]
        // {<Zt>.H }, <Pg>/Z, [<Xn|SP>, <Xm>]
        // {<Zt>.S }, <Pg>/Z, [<Xn|SP>, <Xm>]
        // {<Zt>.D }, <Pg>/Z, [<Xn|SP>, <Xm>]
        case IF_SVE_IN_4A: // ...........mmmmm ...gggnnnnnttttt -- SVE contiguous non-temporal load (scalar plus scalar)
        // {<Zt>.B }, <Pg>/Z, [<Xn|SP>, <Xm>]
        // {<Zt>.H }, <Pg>/Z, [<Xn|SP>, <Xm>]
        // {<Zt>.S }, <Pg>/Z, [<Xn|SP>, <Xm>]
        // {<Zt>.D }, <Pg>/Z, [<Xn|SP>, <Xm>]
        case IF_SVE_IP_4A: // ...........mmmmm ...gggnnnnnttttt -- SVE load and broadcast quadword (scalar plus scalar)
        // {<Zt1>.Q, <Zt2>.Q }, <Pg>/Z, [<Xn|SP>, <Xm>, LSL #4]
        // {<Zt1>.Q, <Zt2>.Q, <Zt3>.Q }, <Pg>/Z, [<Xn|SP>, <Xm>, LSL #4]
        // {<Zt1>.Q, <Zt2>.Q, <Zt3>.Q, <Zt4>.Q }, <Pg>/Z, [<Xn|SP>, <Xm>, LSL #4]
        case IF_SVE_IR_4A: // ...........mmmmm ...gggnnnnnttttt -- SVE load multiple structures (quadwords, scalar plus
                           // scalar)
        // {<Zt1>.B, <Zt2>.B }, <Pg>/Z, [<Xn|SP>, <Xm>]
        // {<Zt1>.H, <Zt2>.H }, <Pg>/Z, [<Xn|SP>, <Xm>, LSL #1]
        // {<Zt1>.S, <Zt2>.S }, <Pg>/Z, [<Xn|SP>, <Xm>, LSL #2]
        // {<Zt1>.D, <Zt2>.D }, <Pg>/Z, [<Xn|SP>, <Xm>, LSL #3]
        // {<Zt1>.B, <Zt2>.B, <Zt3>.B }, <Pg>/Z, [<Xn|SP>, <Xm>]
        // {<Zt1>.H, <Zt2>.H, <Zt3>.H }, <Pg>/Z, [<Xn|SP>, <Xm>, LSL #1]
        // {<Zt1>.S, <Zt2>.S, <Zt3>.S }, <Pg>/Z, [<Xn|SP>, <Xm>, LSL #2]
        // {<Zt1>.D, <Zt2>.D, <Zt3>.D }, <Pg>/Z, [<Xn|SP>, <Xm>, LSL #3]
        // {<Zt1>.B, <Zt2>.B, <Zt3>.B, <Zt4>.B }, <Pg>/Z, [<Xn|SP>, <Xm>]
        // {<Zt1>.H, <Zt2>.H, <Zt3>.H, <Zt4>.H }, <Pg>/Z, [<Xn|SP>, <Xm>, LSL #1]
        // {<Zt1>.S, <Zt2>.S, <Zt3>.S, <Zt4>.S }, <Pg>/Z, [<Xn|SP>, <Xm>, LSL #2]
        // {<Zt1>.D, <Zt2>.D, <Zt3>.D, <Zt4>.D }, <Pg>/Z, [<Xn|SP>, <Xm>, LSL #3]
        case IF_SVE_IT_4A: // ...........mmmmm ...gggnnnnnttttt -- SVE load multiple structures (scalar plus scalar)
        // {<Zt>.D }, <Pg>/Z, [<Xn|SP>, <Zm>.D, LSL #2]
        // {<Zt>.D }, <Pg>/Z, [<Xn|SP>, <Zm>.D, LSL #3]
        case IF_SVE_IU_4B: // ...........mmmmm ...gggnnnnnttttt -- SVE 64-bit gather load (scalar plus 32-bit unpacked
                           // scaled offsets)
        // {<Zt>.D }, <Pg>/Z, [<Xn|SP>, <Zm>.D]
        case IF_SVE_IU_4B_B: // ...........mmmmm ...gggnnnnnttttt -- SVE 64-bit gather load (scalar plus 32-bit unpacked
                             // scaled offsets)
        // {<Zt>.D }, <Pg>/Z, [<Xn|SP>, <Zm>.D]
        case IF_SVE_IU_4B_D: // ...........mmmmm ...gggnnnnnttttt -- SVE 64-bit gather load (scalar plus 32-bit unpacked
                             // scaled offsets)
        // {<Zt>.Q }, <Pg>/Z, [<Zn>.D{, <Xm>}]
        case IF_SVE_IW_4A: // ...........mmmmm ...gggnnnnnttttt -- SVE2 128-bit gather load (vector plus scalar)
        // {<Zt>.D }, <Pg>/Z, [<Zn>.D{, <Xm>}]
        case IF_SVE_IX_4A: // ...........mmmmm ...gggnnnnnttttt -- SVE2 64-bit gather non-temporal load (vector plus
                           // scalar)
        // {<Zt>.Q }, <Pg>, [<Zn>.D{, <Xm>}]
        case IF_SVE_IY_4A: // ...........mmmmm ...gggnnnnnttttt -- SVE2 128-bit scatter store (vector plus scalar)
        // {<Zt>.S }, <Pg>, [<Zn>.S{, <Xm>}]
        case IF_SVE_IZ_4A: // ...........mmmmm ...gggnnnnnttttt -- SVE2 32-bit scatter non-temporal store (vector plus
                           // scalar)
        // {<Zt>.D }, <Pg>, [<Zn>.D{, <Xm>}]
        case IF_SVE_IZ_4A_A: // ...........mmmmm ...gggnnnnnttttt -- SVE2 32-bit scatter non-temporal store (vector plus
                             // scalar)
        // {<Zt>.D }, <Pg>, [<Zn>.D{, <Xm>}]
        case IF_SVE_JA_4A: // ...........mmmmm ...gggnnnnnttttt -- SVE2 64-bit scatter non-temporal store (vector plus
                           // scalar)
        // {<Zt>.B }, <Pg>, [<Xn|SP>, <Xm>]
        // {<Zt>.H }, <Pg>, [<Xn|SP>, <Xm>, LSL #1]
        // {<Zt>.S }, <Pg>, [<Xn|SP>, <Xm>, LSL #2]
        // {<Zt>.D }, <Pg>, [<Xn|SP>, <Xm>, LSL #3]
        case IF_SVE_JB_4A: // ...........mmmmm ...gggnnnnnttttt -- SVE contiguous non-temporal store (scalar plus
                           // scalar)
        // {<Zt1>.B, <Zt2>.B }, <Pg>, [<Xn|SP>, <Xm>]
        // {<Zt1>.H, <Zt2>.H }, <Pg>, [<Xn|SP>, <Xm>, LSL #1]
        // {<Zt1>.S, <Zt2>.S }, <Pg>, [<Xn|SP>, <Xm>, LSL #2]
        // {<Zt1>.D, <Zt2>.D }, <Pg>, [<Xn|SP>, <Xm>, LSL #3]
        // {<Zt1>.B, <Zt2>.B, <Zt3>.B }, <Pg>, [<Xn|SP>, <Xm>]
        // {<Zt1>.H, <Zt2>.H, <Zt3>.H }, <Pg>, [<Xn|SP>, <Xm>, LSL #1]
        // {<Zt1>.S, <Zt2>.S, <Zt3>.S }, <Pg>, [<Xn|SP>, <Xm>, LSL #2]
        // {<Zt1>.D, <Zt2>.D, <Zt3>.D }, <Pg>, [<Xn|SP>, <Xm>, LSL #3]
        // {<Zt1>.B, <Zt2>.B, <Zt3>.B, <Zt4>.B }, <Pg>, [<Xn|SP>, <Xm>]
        // {<Zt1>.H, <Zt2>.H, <Zt3>.H, <Zt4>.H }, <Pg>, [<Xn|SP>, <Xm>, LSL #1]
        // {<Zt1>.S, <Zt2>.S, <Zt3>.S, <Zt4>.S }, <Pg>, [<Xn|SP>, <Xm>, LSL #2]
        // {<Zt1>.D, <Zt2>.D, <Zt3>.D, <Zt4>.D }, <Pg>, [<Xn|SP>, <Xm>, LSL #3]
        case IF_SVE_JC_4A: // ...........mmmmm ...gggnnnnnttttt -- SVE store multiple structures (scalar plus scalar)
        // {<Zt>.Q }, <Pg>, [<Xn|SP>, <Xm>, LSL #2]
        // {<Zt>.D }, <Pg>, [<Xn|SP>, <Xm>, LSL #3]
        case IF_SVE_JD_4C: // ...........mmmmm ...gggnnnnnttttt -- SVE contiguous store (scalar plus scalar)
        // {<Zt>.Q }, <Pg>, [<Xn|SP>, <Xm>, LSL #3]
        case IF_SVE_JD_4C_A: // ...........mmmmm ...gggnnnnnttttt -- SVE contiguous store (scalar plus scalar)
        // {<Zt1>.Q, <Zt2>.Q }, <Pg>, [<Xn|SP>, <Xm>, LSL #4]
        // {<Zt1>.Q, <Zt2>.Q, <Zt3>.Q }, <Pg>, [<Xn|SP>, <Xm>, LSL #4]
        // {<Zt1>.Q, <Zt2>.Q, <Zt3>.Q, <Zt4>.Q }, <Pg>, [<Xn|SP>, <Xm>, LSL #4]
        case IF_SVE_JF_4A: // ...........mmmmm ...gggnnnnnttttt -- SVE store multiple structures (quadwords, scalar plus
                           // scalar)
        // {<Zt>.D }, <Pg>, [<Xn|SP>, <Zm>.D, LSL #1]
        // {<Zt>.D }, <Pg>, [<Xn|SP>, <Zm>.D, LSL #2]
        // {<Zt>.D }, <Pg>, [<Xn|SP>, <Zm>.D, LSL #3]
        case IF_SVE_JJ_4B: // ...........mmmmm ...gggnnnnnttttt -- SVE 64-bit scatter store (scalar plus 64-bit scaled
                           // offsets)
        // {<Zt>.D }, <Pg>, [<Xn|SP>, <Zm>.D]
        case IF_SVE_JJ_4B_C: // ...........mmmmm ...gggnnnnnttttt -- SVE 64-bit scatter store (scalar plus 64-bit scaled
                             // offsets)
        // {<Zt>.D }, <Pg>, [<Xn|SP>, <Zm>.D]
        case IF_SVE_JJ_4B_E: // ...........mmmmm ...gggnnnnnttttt -- SVE 64-bit scatter store (scalar plus 64-bit scaled
                             // offsets)
        // {<Zt>.D }, <Pg>, [<Xn|SP>, <Zm>.D]
        case IF_SVE_JK_4B: // ...........mmmmm ...gggnnnnnttttt -- SVE 64-bit scatter store (scalar plus 64-bit unscaled
                           // offsets)
            emitDispSveConsecutiveRegList(id->idReg1(), insGetSveReg1ListSize(ins), id->idInsOpt(), true); // ttttt
            emitDispPredicateReg(id->idReg2(), insGetPredicateType(fmt), id->idInsOpt(), true);            // ggg
            emitDispSveModAddr(ins, id->idReg3(), id->idReg4(), id->idInsOpt(), fmt);                      // nnnnn
                                                                                                           // mmmmm
            break;

        // {<Zt>.<T>}, <Pg>, [<Xn|SP>{, #<imm>, MUL VL}]
        case IF_SVE_JN_3A: // .........xx.iiii ...gggnnnnnttttt -- SVE contiguous store (scalar plus immediate)
            imm = emitGetInsSC(id);
            emitDispSveConsecutiveRegList(id->idReg1(), insGetSveReg1ListSize(ins), id->idInsOpt(), true); // ttttt
            emitDispPredicateReg(id->idReg2(), insGetPredicateType(fmt), id->idInsOpt(), true);            // ggg
            emitDispSveImmMulVl(id->idReg3(), imm);
            break;

        // {<Zt>.<T>}, <Pg>, [<Xn|SP>{, #<imm>, MUL VL}]
        case IF_SVE_JN_3B: // ..........x.iiii ...gggnnnnnttttt -- SVE contiguous store (scalar plus immediate)
            imm = emitGetInsSC(id);
            emitDispSveConsecutiveRegList(id->idReg1(), insGetSveReg1ListSize(ins), id->idInsOpt(), true); // ttttt
            emitDispPredicateReg(id->idReg2(), insGetPredicateType(fmt), id->idInsOpt(), true);            // ggg
            emitDispSveImmMulVl(id->idReg3(), imm);
            break;

        // <Pt>, [<Xn|SP>{, #<imm>, MUL VL}]
        case IF_SVE_ID_2A: // ..........iiiiii ...iiinnnnn.TTTT -- SVE load predicate register
        // <Pt>, [<Xn|SP>{, #<imm>, MUL VL}]
        case IF_SVE_JG_2A: // ..........iiiiii ...iiinnnnn.TTTT -- SVE store predicate register
            imm = emitGetInsSC(id);
            emitDispPredicateReg(id->idReg1(), insGetPredicateType(fmt), id->idInsOpt(), true); // TTTT
            emitDispSveImmMulVl(id->idReg2(), imm);
            break;

        // <Zt>, [<Xn|SP>{, #<imm>, MUL VL}]
        case IF_SVE_IE_2A: // ..........iiiiii ...iiinnnnnttttt -- SVE load vector register
        // <Zt>, [<Xn|SP>{, #<imm>, MUL VL}]
        case IF_SVE_JH_2A: // ..........iiiiii ...iiinnnnnttttt -- SVE store vector register
            imm = emitGetInsSC(id);
            emitDispReg(id->idReg1(), EA_SCALABLE, true); // ttttt
            emitDispSveImmMulVl(id->idReg2(), imm);
            break;

        // <Zdn>.<T>, <Pg>/M, <Zdn>.<T>, <Zm>.<T>, <const>
        case IF_SVE_GP_3A: // ........xx.....r ...gggmmmmmddddd -- SVE floating-point complex add (predicated)
            imm = emitGetInsSC(id);
            emitDispSveReg(id->idReg1(), id->idInsOpt(), true);
            emitDispPredicateReg(id->idReg2(), insGetPredicateType(fmt), id->idInsOpt(), true);
            emitDispSveReg(id->idReg1(), id->idInsOpt(), true);
            emitDispSveReg(id->idReg3(), id->idInsOpt(), true);
            emitDispImm(emitDecodeRotationImm90_or_270(imm), false);
            break;

        // <Zda>.<T>, <Pg>/M, <Zn>.<T>, <Zm>.<T>, <const>
        case IF_SVE_GT_4A: // ........xx.mmmmm .rrgggnnnnnddddd -- SVE floating-point complex multiply-add (predicated)
            imm = emitGetInsSC(id);
            emitDispSveReg(id->idReg1(), id->idInsOpt(), true);
            emitDispPredicateReg(id->idReg2(), insGetPredicateType(fmt), id->idInsOpt(), true);
            emitDispSveReg(id->idReg3(), id->idInsOpt(), true);
            emitDispSveReg(id->idReg4(), id->idInsOpt(), true);
            emitDispImm(emitDecodeRotationImm0_to_270(imm), false);
            break;

        // <Pd>.<T>, <Pg>/Z, <Zn>.<T>, #0.0
        case IF_SVE_HI_3A: // ........xx...... ...gggnnnnn.DDDD -- SVE floating-point compare with zero
            emitDispPredicateReg(id->idReg1(), insGetPredicateType(fmt, 1), id->idInsOpt(), true);
            emitDispPredicateReg(id->idReg2(), insGetPredicateType(fmt, 2), id->idInsOpt(), true);
            emitDispSveReg(id->idReg3(), id->idInsOpt(), true);
            emitDispFloatZero();
            break;

        // <Zdn>.<T>, <Pg>/M, <Zdn>.<T>, <const>
        case IF_SVE_HM_2A: // ........xx...... ...ggg....iddddd -- SVE floating-point arithmetic with immediate
                           // (predicated)
            imm = emitGetInsSC(id);
            emitDispSveReg(id->idReg1(), id->idInsOpt(), true);
            emitDispPredicateReg(id->idReg2(), insGetPredicateType(fmt), id->idInsOpt(), true);
            emitDispSveReg(id->idReg1(), id->idInsOpt(), true);
            emitDispSmallFloatImm(imm, id->idIns());
            break;

        // <Zdn>.<T>, <Zdn>.<T>, <Zm>.<T>, #<imm>
        case IF_SVE_HN_2A: // ........xx...iii ......mmmmmddddd -- SVE floating-point trig multiply-add coefficient
        case IF_SVE_AW_2A: // ........xx.xxiii ......mmmmmddddd -- sve_int_rotate_imm
            emitDispSveReg(id->idReg1(), id->idInsOpt(), true);
            emitDispSveReg(id->idReg1(), id->idInsOpt(), true);
            emitDispSveReg(id->idReg2(), id->idInsOpt(), true);
            emitDispImm(emitGetInsSC(id), false);
            break;

        // <Zd>.<T>, <Pg>/M, <Zn>.<T>
        case IF_SVE_HP_3A: // .............xx. ...gggnnnnnddddd -- SVE floating-point convert to integer
            emitDispSveReg(id->idReg1(), id->idInsOpt(), true);
            emitDispPredicateReg(id->idReg2(), insGetPredicateType(fmt), id->idInsOpt(), true);
            emitDispSveReg(id->idReg3(), id->idInsOpt(), false);
            break;

        // <Zda>.H, <Pg>/M, <Zn>.H, <Zm>.H
        case IF_SVE_HU_4B: // ...........mmmmm ...gggnnnnnddddd -- SVE floating-point multiply-accumulate writing addend
        // <Zdn>.<T>, <Pg>/M, <Zm>.<T>, <Za>.<T>
        case IF_SVE_HV_4A: // ........xx.aaaaa ...gggmmmmmddddd -- SVE floating-point multiply-accumulate writing
                           // multiplicand
            emitDispSveReg(id->idReg1(), id->idInsOpt(), true);
            emitDispPredicateReg(id->idReg2(), insGetPredicateType(fmt), id->idInsOpt(), true);
            emitDispSveReg(id->idReg3(), id->idInsOpt(), true);
            emitDispSveReg(id->idReg4(), id->idInsOpt(), false);
            break;

        // <Zd>.B, { <Zn>.B }, <Zm>[<index>]
        case IF_SVE_GG_3A: // ........ii.mmmmm ......nnnnnddddd -- SVE2 lookup table with 2-bit indices and 16-bit
                           // element size
        // <Zd>.B, { <Zn>.B }, <Zm>[<index>]
        case IF_SVE_GH_3A: // ........i..mmmmm ......nnnnnddddd -- SVE2 lookup table with 4-bit indices and 16-bit
                           // element size
        // <Zd>.H, { <Zn>.H }, <Zm>[<index>]
        case IF_SVE_GG_3B: // ........ii.mmmmm ...i..nnnnnddddd -- SVE2 lookup table with 2-bit indices and 16-bit
                           // element size
        // <Zd>.H, { <Zn1>.H, <Zn2>.H }, <Zm>[<index>]
        case IF_SVE_GH_3B: // ........ii.mmmmm ......nnnnnddddd -- SVE2 lookup table with 4-bit indices and 16-bit
                           // element size
        // <Zd>.H, {<Zn>.H }, <Zm>[<index>]
        case IF_SVE_GH_3B_B: // ........ii.mmmmm ......nnnnnddddd -- SVE2 lookup table with 4-bit indices and 16-bit
                             // element size
            imm = emitGetInsSC(id);
            emitDispSveReg(id->idReg1(), id->idInsOpt(), true);
            emitDispSveConsecutiveRegList(id->idReg1(), 1, id->idInsOpt(), true);
            emitDispSveReg(id->idReg2(), id->idInsOpt(), false);
            emitDispElementIndex(imm, false);
            break;

        // <prfop>, <Pg>, [<Xn|SP>, <Zm>.S, <mod>]
        // <prfop>, <Pg>, [<Xn|SP>, <Zm>.S, <mod> #1]
        // <prfop>, <Pg>, [<Xn|SP>, <Zm>.S, <mod> #2]
        // <prfop>, <Pg>, [<Xn|SP>, <Zm>.S, <mod> #3]
        case IF_SVE_HY_3A: // .........h.mmmmm ...gggnnnnn.oooo -- SVE 32-bit gather prefetch (scalar plus 32-bit scaled
                           // offsets)
        // <prfop>, <Pg>, [<Xn|SP>, <Zm>.D, <mod>]
        // <prfop>, <Pg>, [<Xn|SP>, <Zm>.D, <mod> #1]
        // <prfop>, <Pg>, [<Xn|SP>, <Zm>.D, <mod> #2]
        // <prfop>, <Pg>, [<Xn|SP>, <Zm>.D, <mod> #3]
        case IF_SVE_HY_3A_A: // .........h.mmmmm ...gggnnnnn.oooo -- SVE 32-bit gather prefetch (scalar plus 32-bit
                             // scaled offsets)
        // <prfop>, <Pg>, [<Xn|SP>, <Zm>.D]
        // <prfop>, <Pg>, [<Xn|SP>, <Zm>.D, LSL #1]
        // <prfop>, <Pg>, [<Xn|SP>, <Zm>.D, LSL #2]
        // <prfop>, <Pg>, [<Xn|SP>, <Zm>.D, LSL #3]
        case IF_SVE_HY_3B: // ...........mmmmm ...gggnnnnn.oooo -- SVE 32-bit gather prefetch (scalar plus 32-bit scaled
                           // offsets)
        // <prfop>, <Pg>, [<Xn|SP>, <Xm>]
        // <prfop>, <Pg>, [<Xn|SP>, <Xm>, LSL #1]
        // <prfop>, <Pg>, [<Xn|SP>, <Xm>, LSL #2]
        // <prfop>, <Pg>, [<Xn|SP>, <Xm>, LSL #3]
        case IF_SVE_IB_3A: // ...........mmmmm ...gggnnnnn.oooo -- SVE contiguous prefetch (scalar plus scalar)
            emitDispSvePrfop(id->idSvePrfop(), true);
            emitDispPredicateReg(id->idReg1(), insGetPredicateType(fmt), id->idInsOpt(), true);
            emitDispSveModAddr(ins, id->idReg2(), id->idReg3(), id->idInsOpt(), fmt);
            break;

        // <prfop>, <Pg>, [<Zn>.S{, #<imm>}]
        // <prfop>, <Pg>, [<Zn>.D{, #<imm>}]
        case IF_SVE_HZ_2A_B: // ...........iiiii ...gggnnnnn.oooo -- SVE 32-bit gather prefetch (vector plus immediate)
            imm = emitGetInsSC(id);
            emitDispSvePrfop(id->idSvePrfop(), true);
            emitDispPredicateReg(id->idReg1(), insGetPredicateType(fmt), id->idInsOpt(), true);
            emitDispSveImm(id->idReg2(), imm, id->idInsOpt());
            break;

        // <prfop>, <Pg>, [<Xn|SP>{, #<imm>, MUL VL}]
        case IF_SVE_IA_2A: // ..........iiiiii ...gggnnnnn.oooo -- SVE contiguous prefetch (scalar plus immediate)
            imm = emitGetInsSC(id);
            emitDispSvePrfop(id->idSvePrfop(), true);
            emitDispPredicateReg(id->idReg1(), insGetPredicateType(fmt), id->idInsOpt(), true);
            emitDispSveImmMulVl(id->idReg2(), imm);
            break;

        // {<Zt>.S }, <Pg>/Z, [<Zn>.S{, #<imm>}]
        // {<Zt>.D }, <Pg>/Z, [<Zn>.D{, #<imm>}]
        case IF_SVE_HX_3A_B: // ...........iiiii ...gggnnnnnttttt -- SVE 32-bit gather load (vector plus immediate)
        // {<Zt>.S }, <Pg>/Z, [<Zn>.S{, #<imm>}]
        // {<Zt>.D }, <Pg>/Z, [<Zn>.D{, #<imm>}]
        case IF_SVE_HX_3A_E: // ...........iiiii ...gggnnnnnttttt -- SVE 32-bit gather load (vector plus immediate)
        // {<Zt>.D }, <Pg>/Z, [<Zn>.D{, #<imm>}]
        case IF_SVE_IV_3A: // ...........iiiii ...gggnnnnnttttt -- SVE 64-bit gather load (vector plus immediate)
        // {<Zt>.S }, <Pg>, [<Zn>.S{, #<imm>}]
        // {<Zt>.D }, <Pg>, [<Zn>.D{, #<imm>}]
        case IF_SVE_JI_3A_A: // ...........iiiii ...gggnnnnnttttt -- SVE 32-bit scatter store (vector plus immediate)
        // {<Zt>.D }, <Pg>, [<Zn>.D{, #<imm>}]
        case IF_SVE_JL_3A: // ...........iiiii ...gggnnnnnttttt -- SVE 64-bit scatter store (vector plus immediate)
        // {<Zt>.D }, <Pg>/Z, [<Xn|SP>{, #<imm>}]
        case IF_SVE_IC_3A: // ..........iiiiii ...gggnnnnnttttt -- SVE load and broadcast element
        // {<Zt>.D }, <Pg>/Z, [<Xn|SP>{, #<imm>}]
        case IF_SVE_IC_3A_A: // ..........iiiiii ...gggnnnnnttttt -- SVE load and broadcast element
        // {<Zt>.D }, <Pg>/Z, [<Xn|SP>{, #<imm>}]
        case IF_SVE_IC_3A_B: // ..........iiiiii ...gggnnnnnttttt -- SVE load and broadcast element
        // {<Zt>.D }, <Pg>/Z, [<Xn|SP>{, #<imm>}]
        case IF_SVE_IC_3A_C: // ..........iiiiii ...gggnnnnnttttt -- SVE load and broadcast element
            imm = emitGetInsSC(id);
            emitDispSveConsecutiveRegList(id->idReg1(), insGetSveReg1ListSize(id->idIns()), id->idInsOpt(), true);
            emitDispPredicateReg(id->idReg2(), insGetPredicateType(fmt), id->idInsOpt(), true);
            emitDispSveImmIndex(id->idReg3(), id->idInsOpt(), imm);
            break;

        // <Zd>, <Zn>
        case IF_SVE_BI_2A: // ................ ......nnnnnddddd -- SVE constructive prefix (unpredicated)
            emitDispSveReg(id->idReg1(), id->idInsOpt(), true);
            emitDispSveReg(id->idReg2(), id->idInsOpt(), false);
            break;

        // <Zd>.<T>, <R><n|SP>
        case IF_SVE_CB_2A: // ........xx...... ......nnnnnddddd -- SVE broadcast general register
            emitDispSveReg(id->idReg1(), id->idInsOpt(), true);
            emitDispReg(encodingZRtoSP(id->idReg2()), size, false);
            break;

        // <Zd>.H, <Zn>.B
        case IF_SVE_HH_2A: // ................ ......nnnnnddddd -- SVE2 FP8 upconverts
        // <Zd>.<T>, <Zn>.<Tb>
        case IF_SVE_CH_2A: // ........xx...... ......nnnnnddddd -- SVE unpack vector elements
            emitDispSveReg(id->idReg1(), (insOpts)(id->idInsOpt() + 1), true);
            emitDispSveReg(id->idReg2(), id->idInsOpt(), false);
            break;

        // <Zd>.<T>, <Zn>.<T>
        case IF_SVE_BJ_2A: // ........xx...... ......nnnnnddddd -- SVE floating-point exponential accelerator
        // <Zd>.<T>, <Zn>.<T>
        case IF_SVE_CG_2A: // ........xx...... ......nnnnnddddd -- SVE reverse vector elements
        // <Zd>.<T>, <Zn>.<T>
        case IF_SVE_HF_2A: // ........xx...... ......nnnnnddddd -- SVE floating-point reciprocal estimate (unpredicated)
            emitDispSveReg(id->idReg1(), id->idInsOpt(), true);
            emitDispSveReg(id->idReg2(), id->idInsOpt(), false);
            break;

        // <Zd>.<T>, <Zn>.<T>, #<const>
        case IF_SVE_BF_2A: // ........xx.xxiii ......nnnnnddddd -- SVE bitwise shift by immediate (unpredicated)
        // <Zd>.<T>, <Zn>.<T>, #<const>
        case IF_SVE_FT_2A: // ........xx.xxiii ......nnnnnddddd -- SVE2 bitwise shift and insert
        // <Zda>.<T>, <Zn>.<T>, #<const>
        case IF_SVE_FU_2A: // ........xx.xxiii ......nnnnnddddd -- SVE2 bitwise shift right and accumulate
            imm = emitGetInsSC(id);
            emitDispSveReg(id->idReg1(), id->idInsOpt(), true);
            emitDispSveReg(id->idReg2(), id->idInsOpt(), true);
            emitDispImm(imm, false);
            break;

        // <Zd>.<T>, <Pg>/Z, #<imm>{, <shift>}
        // <Zd>.<T>, <Pg>/M, #<imm>{, <shift>}
        case IF_SVE_BV_2A:   // ........xx..gggg ..hiiiiiiiiddddd -- SVE copy integer immediate (predicated)
        case IF_SVE_BV_2A_J: // ........xx..gggg ..hiiiiiiiiddddd -- SVE copy integer immediate (predicated)
        {
            imm = emitGetInsSC(id);
            emitDispSveReg(id->idReg1(), id->idInsOpt(), true);                                 // ddddd
            emitDispPredicateReg(id->idReg2(), insGetPredicateType(fmt), id->idInsOpt(), true); // gggg
            emitDispImmOptsLSL(imm, id->idHasShift(), 8);                                       // iiiiiiii, h
            break;
        }

        // <Zd>.<T>, <Pg>/M, #<imm>
        case IF_SVE_BV_2B: // ........xx..gggg ...........ddddd -- SVE copy integer immediate (predicated)
            emitDispSveReg(id->idReg1(), id->idInsOpt(), true);                                 // ddddd
            emitDispPredicateReg(id->idReg2(), insGetPredicateType(fmt), id->idInsOpt(), true); // gggg
            emitDispImm(0, false);
            break;

        // <Zd>.<T>, <Zn>.<T>[<imm>]
        // <Zd>.<T>, <V><n>
        case IF_SVE_BW_2A: // ........ii.xxxxx ......nnnnnddddd -- SVE broadcast indexed element
            imm = emitGetInsSC(id);
            emitDispSveReg(id->idReg1(), id->idInsOpt(), true); // ddddd
            if (imm > 0)
            {
                emitDispSveReg(id->idReg2(), id->idInsOpt(), false); // nnnnn
                emitDispElementIndex(imm, false);
            }
            else
            {
                assert(imm == 0);
                emitDispReg(id->idReg2(), optGetSveElemsize(id->idInsOpt()), false);
            }
            break;

        // <Zd>.<T>, <Zn>.<T>[<imm>]
        case IF_SVE_BX_2A: // ...........ixxxx ......nnnnnddddd -- sve_int_perm_dupq_i
            imm = emitGetInsSC(id);
            emitDispSveReg(id->idReg1(), id->idInsOpt(), true);
            emitDispSveReg(id->idReg2(), id->idInsOpt(), false);
            emitDispElementIndex(imm, false);
            break;

        // <Zdn>.B, <Zdn>.B, <Zm>.B, #<imm>
        case IF_SVE_BY_2A: // ............iiii ......mmmmmddddd -- sve_int_perm_extq
            imm = emitGetInsSC(id);
            emitDispSveReg(id->idReg1(), id->idInsOpt(), true);
            emitDispSveReg(id->idReg1(), id->idInsOpt(), true);
            emitDispSveReg(id->idReg2(), id->idInsOpt(), true);
            emitDispImm(imm, false);
            break;

        default:
            printf("unexpected format %s", emitIfName(id->idInsFmt()));
            assert(!"unexpectedFormat");
            break;
    }
}

#if defined(DEBUG) || defined(LATE_DISASM)
//----------------------------------------------------------------------------------------
// getInsSveExecutionCharacteristics:
//    Returns the current SVE instruction's execution characteristics
//
// Arguments:
//    id  - The current instruction descriptor to be evaluated
//    result - out parameter for execution characteristics struct
//    (only insLatency and insThroughput will be set)
//
// Notes:
//    SVE latencies from Arm Neoverse N2 Software Optimization Guide, Issue 5.0, Revision: r0p3
//
void emitter::getInsSveExecutionCharacteristics(instrDesc* id, insExecutionCharacteristics& result)
{
    instruction ins = id->idIns();
    switch (id->idInsFmt())
    {
        case IF_SVE_AA_3A: // ........xx...... ...gggmmmmmddddd
            switch (ins)
            {
                case INS_sve_add:
                case INS_sve_sub:
                case INS_sve_subr:
                case INS_sve_sabd:
                case INS_sve_smax:
                case INS_sve_smin:
                case INS_sve_uabd:
                case INS_sve_umax:
                case INS_sve_umin:
                case INS_sve_shadd:
                case INS_sve_shsub:
                case INS_sve_shsubr:
                case INS_sve_srhadd:
                case INS_sve_uhadd:
                case INS_sve_uhsub:
                case INS_sve_uhsubr:
                case INS_sve_urhadd:
                case INS_sve_addp:
                case INS_sve_smaxp:
                case INS_sve_sminp:
                case INS_sve_umaxp:
                case INS_sve_uminp:
                case INS_sve_sqadd:
                case INS_sve_sqsub:
                case INS_sve_uqadd:
                case INS_sve_uqsub:
                case INS_sve_sqsubr:
                case INS_sve_suqadd:
                case INS_sve_uqsubr:
                case INS_sve_usqadd:
                    result.insLatency    = PERFSCORE_LATENCY_2C;
                    result.insThroughput = PERFSCORE_THROUGHPUT_2X;
                    break;

                case INS_sve_mul:
                case INS_sve_smulh:
                case INS_sve_umulh:
                case INS_sve_sqrshl:
                case INS_sve_sqrshlr:
                case INS_sve_sqshl:
                case INS_sve_sqshlr:
                case INS_sve_srshl:
                case INS_sve_srshlr:
                case INS_sve_uqrshl:
                case INS_sve_uqrshlr:
                case INS_sve_uqshl:
                case INS_sve_uqshlr:
                case INS_sve_urshl:
                case INS_sve_urshlr:
                    result.insLatency    = PERFSCORE_LATENCY_4C;
                    result.insThroughput = PERFSCORE_THROUGHPUT_1C;
                    break;

                case INS_sve_asrr:
                case INS_sve_lslr:
                case INS_sve_lsrr:
                case INS_sve_asr:
                case INS_sve_lsl:
                case INS_sve_lsr:
                    result.insLatency    = PERFSCORE_LATENCY_2C;
                    result.insThroughput = PERFSCORE_THROUGHPUT_1C;
                    break;

                default:
                    result.insLatency    = PERFSCORE_LATENCY_1C;
                    result.insThroughput = PERFSCORE_THROUGHPUT_1C;
                    break;
            }
            break;

        // Divides, 32 bit (Note: worse for 64 bit)
        case IF_SVE_AC_3A: // ........xx...... ...gggmmmmmddddd -- SVE integer divide vectors (predicated)
            result.insLatency    = PERFSCORE_LATENCY_12C;    // 7 to 12
            result.insThroughput = PERFSCORE_THROUGHPUT_11C; // 1/11 to 1/7
            break;

        // Reduction, logical
        case IF_SVE_AF_3A: // ........xx...... ...gggnnnnnddddd -- SVE bitwise logical reduction (predicated)
            result.insLatency    = PERFSCORE_LATENCY_6C;
            result.insThroughput = PERFSCORE_THROUGHPUT_1C;
            break;

        case IF_SVE_AH_3A: // ........xx.....M ...gggnnnnnddddd -- SVE constructive prefix (predicated)
            result.insThroughput = PERFSCORE_THROUGHPUT_2C;
            result.insLatency    = PERFSCORE_LATENCY_2C;
            break;

        // Reduction, arithmetic, D form (worse for B, S and H)
        case IF_SVE_AI_3A: // ........xx...... ...gggnnnnnddddd -- SVE integer add reduction (predicated)
        // Reduction, arithmetic, D form (worse for B, S and H)
        case IF_SVE_AK_3A: // ........xx...... ...gggnnnnnddddd -- SVE integer min/max reduction (predicated)
            result.insLatency    = PERFSCORE_LATENCY_4C;
            result.insThroughput = PERFSCORE_THROUGHPUT_1C;
            break;

        case IF_SVE_AM_2A: // ........xx...... ...gggxxiiiddddd -- SVE bitwise shift by immediate (predicated)
            switch (ins)
            {
                case INS_sve_asr:
                case INS_sve_lsl:
                case INS_sve_lsr:
                    result.insThroughput = PERFSCORE_THROUGHPUT_1C;
                    result.insLatency    = PERFSCORE_LATENCY_2C;
                    break;
                case INS_sve_srshr:
                case INS_sve_sqshl:
                case INS_sve_urshr:
                case INS_sve_sqshlu:
                case INS_sve_uqshl:
                case INS_sve_asrd:
                    result.insThroughput = PERFSCORE_THROUGHPUT_1C;
                    result.insLatency    = PERFSCORE_LATENCY_4C;
                    break;
                default:
                    // all other instructions
                    perfScoreUnhandledInstruction(id, &result);
                    break;
            }
            break;

        // Arithmetic, shift
        case IF_SVE_AO_3A: // ........xx...... ...gggmmmmmddddd -- SVE bitwise shift by wide elements (predicated)
            result.insLatency    = PERFSCORE_LATENCY_2C;
            result.insThroughput = PERFSCORE_THROUGHPUT_1C;
            break;

        // Count/reverse bits
        // Arithmetic, basic
        // Floating point absolute value/difference
        // Floating point arithmetic
        // Logical
        case IF_SVE_AP_3A: // ........xx...... ...gggnnnnnddddd -- SVE bitwise unary operations (predicated)
            result.insLatency    = PERFSCORE_LATENCY_2C;
            result.insThroughput = PERFSCORE_THROUGHPUT_2X;
            break;

        case IF_SVE_AQ_3A:
            switch (ins)
            {
                // Arithmetic, basic
                case INS_sve_abs:
                case INS_sve_neg:
                    result.insLatency    = PERFSCORE_LATENCY_2C;
                    result.insThroughput = PERFSCORE_THROUGHPUT_2X;
                    break;

                // Extend, sign or zero
                case INS_sve_sxtb:
                case INS_sve_sxth:
                case INS_sve_sxtw:
                case INS_sve_uxtb:
                case INS_sve_uxth:
                case INS_sve_uxtw:
                    result.insLatency    = PERFSCORE_LATENCY_2C;
                    result.insThroughput = PERFSCORE_THROUGHPUT_1C;
                    break;

                default:
                    // all other instructions
                    perfScoreUnhandledInstruction(id, &result);
                    break;
            }
            break;

        case IF_SVE_AR_4A: // ........xx.mmmmm ...gggnnnnnddddd -- SVE integer multiply-accumulate writing addend
                           // (predicated)
        case IF_SVE_AS_4A: // ........xx.mmmmm ...gggaaaaaddddd -- SVE integer multiply-add writing multiplicand
                           // (predicated)
        case IF_SVE_FD_3A: // .........i.iimmm ......nnnnnddddd -- SVE2 integer multiply (indexed)
        case IF_SVE_FD_3B: // ...........iimmm ......nnnnnddddd -- SVE2 integer multiply (indexed)
        case IF_SVE_FD_3C: // ...........immmm ......nnnnnddddd -- SVE2 integer multiply (indexed)
        case IF_SVE_FF_3A: // .........i.iimmm ......nnnnnddddd -- SVE2 integer multiply-add (indexed)
        case IF_SVE_FF_3B: // ...........iimmm ......nnnnnddddd -- SVE2 integer multiply-add (indexed)
        case IF_SVE_FF_3C: // ...........immmm ......nnnnnddddd -- SVE2 integer multiply-add (indexed)
        case IF_SVE_FI_3A: // .........i.iimmm ......nnnnnddddd -- SVE2 saturating multiply high (indexed)
        case IF_SVE_FI_3B: // ...........iimmm ......nnnnnddddd -- SVE2 saturating multiply high (indexed)
        case IF_SVE_FI_3C: // ...........immmm ......nnnnnddddd -- SVE2 saturating multiply high (indexed)
        case IF_SVE_FK_3A: // .........i.iimmm ......nnnnnddddd -- SVE2 saturating multiply-add high (indexed)
        case IF_SVE_FK_3B: // ...........iimmm ......nnnnnddddd -- SVE2 saturating multiply-add high (indexed)
        case IF_SVE_FK_3C: // ...........immmm ......nnnnnddddd -- SVE2 saturating multiply-add high (indexed)
        case IF_SVE_EM_3A: // ........xx.mmmmm ......nnnnnddddd -- SVE2 saturating multiply-add high
            result.insThroughput = PERFSCORE_THROUGHPUT_2X;
            result.insLatency    = PERFSCORE_LATENCY_5C;
            break;

        case IF_SVE_GU_3A: // ...........iimmm ......nnnnnddddd -- SVE floating-point multiply-add (indexed)
        case IF_SVE_GU_3B: // ...........immmm ......nnnnnddddd -- SVE floating-point multiply-add (indexed)
        case IF_SVE_GN_3A: // ...........mmmmm ......nnnnnddddd -- SVE2 FP8 multiply-add long
            result.insThroughput = PERFSCORE_THROUGHPUT_2C;
            result.insLatency    = PERFSCORE_LATENCY_4C;
            break;

        case IF_SVE_GX_3A: // ...........iimmm ......nnnnnddddd -- SVE floating-point multiply (indexed)
        case IF_SVE_GX_3B: // ...........immmm ......nnnnnddddd -- SVE floating-point multiply (indexed)
            result.insThroughput = PERFSCORE_THROUGHPUT_2C;
            result.insLatency    = PERFSCORE_LATENCY_3C;
            break;

        case IF_SVE_GY_3B: // ...........iimmm ......nnnnnddddd -- SVE BFloat16 floating-point dot product (indexed)
            switch (ins)
            {
                case INS_sve_fdot:
                    result.insThroughput = PERFSCORE_THROUGHPUT_1C; // need to fix
                    result.insLatency    = PERFSCORE_LATENCY_1C;    // need to fix
                    break;
                case INS_sve_bfdot:
                    result.insThroughput = PERFSCORE_THROUGHPUT_2C;
                    result.insLatency    = PERFSCORE_LATENCY_4C;
                    break;
                default:
                    // all other instructions
                    perfScoreUnhandledInstruction(id, &result);
                    break;
            }
            break;

        case IF_SVE_HA_3A: // ...........mmmmm ......nnnnnddddd -- SVE BFloat16 floating-point dot product
            switch (ins)
            {
                case INS_sve_fdot:
                    result.insThroughput = PERFSCORE_THROUGHPUT_1C; // need to fix
                    result.insLatency    = PERFSCORE_LATENCY_1C;    // need to fix
                    break;
                case INS_sve_bfdot:
                    result.insThroughput = PERFSCORE_THROUGHPUT_2C;
                    result.insLatency    = PERFSCORE_LATENCY_4C;
                    break;
                default:
                    // all other instructions
                    perfScoreUnhandledInstruction(id, &result);
                    break;
            }
            break;

        case IF_SVE_HB_3A: // ...........mmmmm ......nnnnnddddd -- SVE floating-point multiply-add long
            switch (ins)
            {
                case INS_sve_fmlalb:
                case INS_sve_fmlalt:
                case INS_sve_fmlslb:
                case INS_sve_fmlslt:
                case INS_sve_bfmlalb:
                case INS_sve_bfmlalt:
                    result.insThroughput = PERFSCORE_THROUGHPUT_2C;
                    result.insLatency    = PERFSCORE_LATENCY_4C;
                    break;
                case INS_sve_bfmlslb:
                case INS_sve_bfmlslt:
                    result.insThroughput = PERFSCORE_THROUGHPUT_1C; // need to fix
                    result.insLatency    = PERFSCORE_LATENCY_1C;    // need to fix
                    break;
                default:
                    // all other instructions
                    perfScoreUnhandledInstruction(id, &result);
                    break;
            }
            break;

        case IF_SVE_AV_3A: // ...........mmmmm ......kkkkkddddd -- SVE2 bitwise ternary operations
            switch (ins)
            {
                case INS_sve_eor3:
                case INS_sve_bcax:
                    result.insThroughput = PERFSCORE_THROUGHPUT_1C;
                    result.insLatency    = PERFSCORE_LATENCY_2C;
                    break;
                case INS_sve_bsl:
                case INS_sve_bsl1n:
                case INS_sve_bsl2n:
                case INS_sve_nbsl:
                    result.insThroughput = PERFSCORE_THROUGHPUT_2C;
                    result.insLatency    = PERFSCORE_LATENCY_2C;
                    break;
                default:
                    // all other instructions
                    perfScoreUnhandledInstruction(id, &result);
                    break;
            }
            break;

        case IF_SVE_GU_3C:   // .........i.iimmm ......nnnnnddddd -- SVE floating-point multiply-add (indexed)
        case IF_SVE_GX_3C:   // .........i.iimmm ......nnnnnddddd -- SVE floating-point multiply (indexed)
        case IF_SVE_EW_3A:   // ...........mmmmm ......nnnnnddddd -- SVE2 multiply-add (checked pointer)
        case IF_SVE_EW_3B:   // ...........mmmmm ......aaaaaddddd -- SVE2 multiply-add (checked pointer)
        case IF_SVE_EX_3A:   // ........xx.mmmmm ......nnnnnddddd -- SVE permute vector elements (quadwords)
        case IF_SVE_AT_3B:   // ...........mmmmm ......nnnnnddddd -- SVE integer add/subtract vectors (unpredicated)
        case IF_SVE_AB_3B:   // ................ ...gggmmmmmddddd -- SVE integer add/subtract vectors (predicated)
        case IF_SVE_HL_3B:   // ................ ...gggmmmmmddddd -- SVE floating-point arithmetic (predicated)
        case IF_SVE_GO_3A:   // ...........mmmmm ......nnnnnddddd -- SVE2 FP8 multiply-add long long
        case IF_SVE_GW_3B:   // ...........mmmmm ......nnnnnddddd -- SVE FP clamp
        case IF_SVE_HA_3A_E: // ...........mmmmm ......nnnnnddddd -- SVE BFloat16 floating-point dot product
        case IF_SVE_HA_3A_F: // ...........mmmmm ......nnnnnddddd -- SVE BFloat16 floating-point dot product
        case IF_SVE_HD_3A_A: // ...........mmmmm ......nnnnnddddd -- SVE floating point matrix multiply accumulate
        case IF_SVE_HK_3B:   // ...........mmmmm ......nnnnnddddd -- SVE floating-point arithmetic (unpredicated)
            result.insThroughput = PERFSCORE_THROUGHPUT_1C; // need to fix
            result.insLatency    = PERFSCORE_LATENCY_1C;    // need to fix
            break;

        case IF_SVE_AT_3A: // ........xx.mmmmm ......nnnnnddddd
            switch (ins)
            {
                case INS_sve_tbxq:
                case INS_sve_sclamp:
                case INS_sve_uclamp:
                case INS_sve_fclamp:
                    result.insThroughput = PERFSCORE_THROUGHPUT_1C; // need to fix
                    result.insLatency    = PERFSCORE_LATENCY_1C;    // need to fix
                    break;

                case INS_sve_bext:
                case INS_sve_bdep:
                case INS_sve_bgrp:
                    result.insThroughput = PERFSCORE_THROUGHPUT_2X;
                    result.insLatency    = PERFSCORE_LATENCY_6C;
                    break;

                case INS_sve_ftssel:
                case INS_sve_fmul:
                case INS_sve_ftsmul:
                    result.insThroughput = PERFSCORE_THROUGHPUT_2C;
                    result.insLatency    = PERFSCORE_LATENCY_3C;
                    break;

                case INS_sve_frecps:
                case INS_sve_frsqrts:
                    result.insThroughput = PERFSCORE_THROUGHPUT_2C;
                    result.insLatency    = PERFSCORE_LATENCY_4C;
                    break;

                case INS_sve_mul:
                case INS_sve_smulh:
                case INS_sve_umulh:
                case INS_sve_sqdmulh:
                case INS_sve_sqrdmulh:
                    result.insThroughput = PERFSCORE_THROUGHPUT_2X;
                    result.insLatency    = PERFSCORE_LATENCY_5C;
                    break;

                default:
                    result.insThroughput = PERFSCORE_THROUGHPUT_2C;
                    result.insLatency    = PERFSCORE_LATENCY_2C;
                    break;
            }
            break;

        case IF_SVE_FL_3A: // ........xx.mmmmm ......nnnnnddddd
            switch (ins)
            {
                case INS_sve_smullb:
                case INS_sve_smullt:
                case INS_sve_umullb:
                case INS_sve_umullt:
                case INS_sve_sqdmullb:
                case INS_sve_sqdmullt:
                    result.insThroughput = PERFSCORE_THROUGHPUT_1C;
                    result.insLatency    = PERFSCORE_LATENCY_4C;
                    break;

                case INS_sve_pmullb:
                case INS_sve_pmullt:
                    result.insThroughput = PERFSCORE_THROUGHPUT_1C;
                    result.insLatency    = PERFSCORE_LATENCY_2C;
                    break;

                default:
                    result.insThroughput = PERFSCORE_THROUGHPUT_2C;
                    result.insLatency    = PERFSCORE_LATENCY_2C;
                    break;
            }
            break;

        case IF_SVE_BR_3B:   // ...........mmmmm ......nnnnnddddd -- SVE permute vector segments
        case IF_SVE_BZ_3A:   // ........xx.mmmmm ......nnnnnddddd -- SVE table lookup (three sources)
        case IF_SVE_BZ_3A_A: // ........xx.mmmmm ......nnnnnddddd -- SVE table lookup (three sources)
        case IF_SVE_FM_3A:   // ........xx.mmmmm ......nnnnnddddd -- SVE2 integer add/subtract wide
        case IF_SVE_GC_3A:   // ........xx.mmmmm ......nnnnnddddd -- SVE2 integer add/subtract narrow high part
        case IF_SVE_GF_3A:   // ........xx.mmmmm ......nnnnnddddd -- SVE2 histogram generation (segment)
        case IF_SVE_AU_3A:   // ...........mmmmm ......nnnnnddddd -- SVE bitwise logical operations (unpredicated)
        case IF_SVE_GI_4A:   // ........xx.mmmmm ...gggnnnnnddddd -- SVE2 histogram generation (vector)
        case IF_SVE_BB_2A:   // ...........nnnnn .....iiiiiiddddd -- SVE stack frame adjustment
        case IF_SVE_BC_1A:   // ................ .....iiiiiiddddd -- SVE stack frame size
            result.insThroughput = PERFSCORE_THROUGHPUT_2C;
            result.insLatency    = PERFSCORE_LATENCY_2C;
            break;

        case IF_SVE_BA_3A: // ........xx.mmmmm ......nnnnnddddd -- SVE index generation (register start, register
                           // increment)
        case IF_SVE_AX_1A: // ........xx.iiiii ......iiiiiddddd -- SVE index generation (immediate start, immediate
                           // increment)
        case IF_SVE_AY_2A: // ........xx.mmmmm ......iiiiiddddd -- SVE index generation (immediate start, register
                           // increment)
        case IF_SVE_AZ_2A: // ........xx.iiiii ......nnnnnddddd -- SVE index generation (register start, immediate
                           // increment)
            result.insThroughput = PERFSCORE_THROUGHPUT_2X;
            result.insLatency    = PERFSCORE_LATENCY_8C;
            break;

        case IF_SVE_BH_3A:   // .........x.mmmmm ....hhnnnnnddddd -- SVE address generation
        case IF_SVE_BH_3B:   // ...........mmmmm ....hhnnnnnddddd -- SVE address generation
        case IF_SVE_BH_3B_A: // ...........mmmmm ....hhnnnnnddddd -- SVE address generation
            result.insThroughput = PERFSCORE_THROUGHPUT_2C;
            result.insLatency    = PERFSCORE_LATENCY_2C;
            break;

        case IF_SVE_BL_1A: // ............iiii ......pppppddddd -- SVE element count
        case IF_SVE_BM_1A: // ............iiii ......pppppddddd -- SVE inc/dec register by element count
        case IF_SVE_BN_1A: // ............iiii ......pppppddddd -- SVE inc/dec vector by element count
        case IF_SVE_BO_1A: // ...........Xiiii ......pppppddddd -- SVE saturating inc/dec register by element count
        case IF_SVE_BP_1A: // ............iiii ......pppppddddd -- SVE saturating inc/dec vector by element count
        case IF_SVE_BQ_2A: // ...........iiiii ...iiinnnnnddddd -- SVE extract vector (immediate offset, destructive)
        case IF_SVE_BQ_2B: // ...........iiiii ...iiimmmmmddddd -- SVE extract vector (immediate offset, destructive)
        case IF_SVE_BU_2A: // ........xx..gggg ...iiiiiiiiddddd -- SVE copy floating-point immediate (predicated)
        case IF_SVE_BS_1A: // ..............ii iiiiiiiiiiiddddd -- SVE bitwise logical with immediate (unpredicated)
        case IF_SVE_BT_1A: // ..............ii iiiiiiiiiiiddddd -- SVE broadcast bitmask immediate
            result.insThroughput = PERFSCORE_THROUGHPUT_2C;
            result.insLatency    = PERFSCORE_LATENCY_2C;
            break;

        case IF_SVE_FO_3A: // ...........mmmmm ......nnnnnddddd -- SVE integer matrix multiply accumulate
            result.insThroughput = PERFSCORE_THROUGHPUT_2C;
            result.insLatency    = PERFSCORE_LATENCY_3C;
            break;

        case IF_SVE_BG_3A: // ........xx.mmmmm ......nnnnnddddd -- SVE bitwise shift by wide elements (unpredicated)
        case IF_SVE_FN_3B: // ...........mmmmm ......nnnnnddddd -- SVE2 integer multiply long
        case IF_SVE_BD_3B: // ...........mmmmm ......nnnnnddddd -- SVE2 integer multiply vectors (unpredicated)
        case IF_SVE_AW_2A: // ........xx.xxiii ......mmmmmddddd -- sve_int_rotate_imm
            result.insThroughput = PERFSCORE_THROUGHPUT_1C;
            result.insLatency    = PERFSCORE_LATENCY_2C;
            break;

        case IF_SVE_BV_2A:   // ........xx..gggg ..hiiiiiiiiddddd -- SVE copy integer immediate (predicated)
        case IF_SVE_BV_2A_J: // ........xx..gggg ..hiiiiiiiiddddd -- SVE copy integer immediate (predicated)
        case IF_SVE_BV_2B:   // ........xx..gggg ...........ddddd -- SVE copy integer immediate (predicated)
            result.insThroughput = PERFSCORE_THROUGHPUT_2C;
            result.insLatency    = PERFSCORE_LATENCY_2C;
            break;

        case IF_SVE_BW_2A: // ........ii.xxxxx ......nnnnnddddd -- SVE broadcast indexed element
            result.insThroughput = PERFSCORE_THROUGHPUT_1C;
            result.insLatency    = PERFSCORE_LATENCY_3C;
            break;

        case IF_SVE_CE_2A: // ................ ......nnnnn.DDDD -- SVE move predicate from vector
        case IF_SVE_CE_2B: // .........i...ii. ......nnnnn.DDDD -- SVE move predicate from vector
        case IF_SVE_CE_2C: // ..............i. ......nnnnn.DDDD -- SVE move predicate from vector
        case IF_SVE_CE_2D: // .............ii. ......nnnnn.DDDD -- SVE move predicate from vector
        case IF_SVE_CF_2A: // ................ .......NNNNddddd -- SVE move predicate into vector
        case IF_SVE_CF_2B: // .........i...ii. .......NNNNddddd -- SVE move predicate into vector
        case IF_SVE_CF_2C: // ..............i. .......NNNNddddd -- SVE move predicate into vector
        case IF_SVE_CF_2D: // .............ii. .......NNNNddddd -- SVE move predicate into vector
            result.insThroughput = PERFSCORE_THROUGHPUT_140C; // @ToDo currently undocumented
            result.insLatency    = PERFSCORE_LATENCY_140C;
            break;

        case IF_SVE_CC_2A: // ........xx...... ......mmmmmddddd -- SVE insert SIMD&FP scalar register
        case IF_SVE_CD_2A: // ........xx...... ......mmmmmddddd -- SVE insert general register
            result.insThroughput = PERFSCORE_THROUGHPUT_1C;
            result.insLatency    = PERFSCORE_LATENCY_5C;
            break;

        case IF_SVE_CI_3A: // ........xx..MMMM .......NNNN.DDDD -- SVE permute predicate elements
        case IF_SVE_CJ_2A: // ........xx...... .......NNNN.DDDD -- SVE reverse predicate elements
        case IF_SVE_CK_2A: // ................ .......NNNN.DDDD -- SVE unpack predicate elements
            result.insThroughput = PERFSCORE_THROUGHPUT_2C;
            result.insLatency    = PERFSCORE_LATENCY_2C;
            break;

        // Conditional extract operations, SIMD&FP scalar and vector forms
        case IF_SVE_CL_3A: // ........xx...... ...gggnnnnnddddd -- SVE compress active elements
        case IF_SVE_CM_3A: // ........xx...... ...gggmmmmmddddd -- SVE conditionally broadcast element to vector
        case IF_SVE_CN_3A: // ........xx...... ...gggmmmmmddddd -- SVE conditionally extract element to SIMD&FP scalar
            result.insLatency    = PERFSCORE_LATENCY_3C;
            result.insThroughput = PERFSCORE_THROUGHPUT_1C;
            break;

        // Conditional extract operations, scalar form
        case IF_SVE_CO_3A: // ........xx...... ...gggmmmmmddddd -- SVE conditionally extract element to general register
            result.insLatency    = PERFSCORE_LATENCY_8C;
            result.insThroughput = PERFSCORE_THROUGHPUT_1C;
            break;

        // Copy, scalar SIMD&FP or imm
        case IF_SVE_CP_3A: // ........xx...... ...gggnnnnnddddd -- SVE copy SIMD&FP scalar register to vector
                           // (predicated)
            result.insLatency    = PERFSCORE_LATENCY_2C;
            result.insThroughput = PERFSCORE_THROUGHPUT_2X;
            break;

        // Copy, scalar
        case IF_SVE_CQ_3A: // ........xx...... ...gggnnnnnddddd -- SVE copy general register to vector (predicated)
            result.insLatency    = PERFSCORE_LATENCY_5C;
            result.insThroughput = PERFSCORE_THROUGHPUT_1C;
            break;

        case IF_SVE_CT_3A: // ................ ...gggnnnnnddddd -- SVE reverse doublewords
            result.insThroughput = PERFSCORE_THROUGHPUT_140C; // @ToDo Currently undocumented.
            result.insLatency    = PERFSCORE_LATENCY_140C;
            break;

        case IF_SVE_CV_3A: // ........xx...... ...VVVnnnnnddddd -- SVE vector splice (constructive)
        case IF_SVE_CV_3B: // ........xx...... ...VVVmmmmmddddd -- SVE vector splice (destructive)
            result.insLatency    = PERFSCORE_LATENCY_3C;
            result.insThroughput = PERFSCORE_THROUGHPUT_1C;
            break;

        case IF_SVE_CW_4A: // ........xx.mmmmm ..VVVVnnnnnddddd -- SVE select vector elements (predicated)
            result.insLatency    = PERFSCORE_LATENCY_2C;
            result.insThroughput = PERFSCORE_THROUGHPUT_2C;
            break;

        case IF_SVE_CX_4A:   // ........xx.mmmmm ...gggnnnnn.DDDD -- SVE integer compare vectors
        case IF_SVE_CX_4A_A: // ........xx.mmmmm ...gggnnnnn.DDDD -- SVE integer compare vectors
        case IF_SVE_CY_3A:   // ........xx.iiiii ...gggnnnnn.DDDD -- SVE integer compare with signed immediate
        case IF_SVE_CY_3B:   // ........xx.iiiii ii.gggnnnnn.DDDD -- SVE integer compare with unsigned immediate
        case IF_SVE_EG_3A:   // ...........iimmm ......nnnnnddddd -- SVE two-way dot product (indexed)
        case IF_SVE_EY_3A:   // ...........iimmm ......nnnnnddddd -- SVE integer dot product (indexed)
        case IF_SVE_EY_3B:   // ...........immmm ......nnnnnddddd -- SVE integer dot product (indexed)
        case IF_SVE_FE_3A:   // ...........iimmm ....i.nnnnnddddd -- SVE2 integer multiply long (indexed)
        case IF_SVE_FE_3B:   // ...........immmm ....i.nnnnnddddd -- SVE2 integer multiply long (indexed)
        case IF_SVE_FG_3A:   // ...........iimmm ....i.nnnnnddddd -- SVE2 integer multiply-add long (indexed)
        case IF_SVE_FG_3B:   // ...........immmm ....i.nnnnnddddd -- SVE2 integer multiply-add long (indexed)
        case IF_SVE_FH_3A:   // ...........iimmm ....i.nnnnnddddd -- SVE2 saturating multiply (indexed)
        case IF_SVE_FH_3B:   // ...........immmm ....i.nnnnnddddd -- SVE2 saturating multiply (indexed)
        case IF_SVE_FJ_3A:   // ...........iimmm ....i.nnnnnddddd -- SVE2 saturating multiply-add (indexed)
        case IF_SVE_FJ_3B:   // ...........immmm ....i.nnnnnddddd -- SVE2 saturating multiply-add (indexed)
        case IF_SVE_EH_3A:   // ........xx.mmmmm ......nnnnnddddd -- SVE integer dot product (unpredicated)
        case IF_SVE_EL_3A:   // ........xx.mmmmm ......nnnnnddddd
        case IF_SVE_FW_3A:   // ........xx.mmmmm ......nnnnnddddd -- SVE2 integer absolute difference and accumulate
            result.insLatency    = PERFSCORE_LATENCY_4C;
            result.insThroughput = PERFSCORE_THROUGHPUT_1C;
            break;

        case IF_SVE_GJ_3A: // ...........mmmmm ......nnnnnddddd -- SVE2 crypto constructive binary operations
            switch (ins)
            {
                case INS_sve_rax1:
                    result.insThroughput = PERFSCORE_THROUGHPUT_1C;
                    result.insLatency    = PERFSCORE_LATENCY_2C;
                    break;
                case INS_sve_sm4ekey:
                    result.insThroughput = PERFSCORE_THROUGHPUT_1C;
                    result.insLatency    = PERFSCORE_LATENCY_4C;
                    break;
                default:
                    // all other instructions
                    perfScoreUnhandledInstruction(id, &result);
                    break;
            }
            break;

        case IF_SVE_GZ_3A: // ...........iimmm ....i.nnnnnddddd -- SVE floating-point multiply-add long (indexed)
            switch (ins)
            {
                case INS_sve_fmlalb:
                case INS_sve_fmlalt:
                case INS_sve_fmlslb:
                case INS_sve_fmlslt:
                case INS_sve_bfmlalb:
                case INS_sve_bfmlalt:
                    result.insThroughput = PERFSCORE_THROUGHPUT_2C;
                    result.insLatency    = PERFSCORE_LATENCY_4C;
                    break;
                case INS_sve_bfmlslb:
                case INS_sve_bfmlslt:
                    result.insThroughput = PERFSCORE_THROUGHPUT_1C; // need to fix
                    result.insLatency    = PERFSCORE_LATENCY_1C;    // need to fix
                    break;
                default:
                    // all other instructions
                    perfScoreUnhandledInstruction(id, &result);
                    break;
            }
            break;

        case IF_SVE_EZ_3A: // ...........iimmm ......nnnnnddddd -- SVE mixed sign dot product (indexed)
            result.insLatency    = PERFSCORE_LATENCY_3C;
            result.insThroughput = PERFSCORE_THROUGHPUT_2C;
            break;

        case IF_SVE_CZ_4A:   // ............MMMM ..gggg.NNNN.DDDD -- SVE predicate logical operations
        case IF_SVE_CZ_4A_A: // ............MMMM ..gggg.NNNN.DDDD -- SVE predicate logical operations
        case IF_SVE_CZ_4A_K: // ............MMMM ..gggg.NNNN.DDDD -- SVE predicate logical operations
        case IF_SVE_CZ_4A_L: // ............MMMM ..gggg.NNNN.DDDD -- SVE predicate logical operations
            switch (ins)
            {
                case INS_sve_mov:
                case INS_sve_and:
                case INS_sve_orr:
                case INS_sve_eor:
                case INS_sve_bic:
                case INS_sve_orn:
                case INS_sve_not:
                case INS_sve_sel:
                    result.insLatency    = PERFSCORE_LATENCY_2C;
                    result.insThroughput = PERFSCORE_THROUGHPUT_2C;
                    break;

                case INS_sve_bics:
                case INS_sve_eors:
                case INS_sve_nots:
                case INS_sve_ands:
                case INS_sve_orrs:
                case INS_sve_orns:
                case INS_sve_nors:
                case INS_sve_nands:
                    result.insLatency    = PERFSCORE_LATENCY_2C;
                    result.insThroughput = PERFSCORE_THROUGHPUT_1C;
                    break;

                case INS_sve_nor:
                case INS_sve_nand:
                    result.insLatency    = PERFSCORE_LATENCY_1C;
                    result.insThroughput = PERFSCORE_THROUGHPUT_1C;
                    break;

                case INS_sve_movs:
                    result.insLatency    = PERFSCORE_LATENCY_1C;
                    result.insThroughput = PERFSCORE_THROUGHPUT_3C;
                    break;

                default:
                    // all other instructions
                    perfScoreUnhandledInstruction(id, &result);
                    break;
            }
            break;

        case IF_SVE_DA_4A: // ............MMMM ..gggg.NNNN.DDDD -- SVE propagate break from previous partition
        case IF_SVE_DC_3A: // ................ ..gggg.NNNN.MMMM -- SVE propagate break to next partition
            switch (ins)
            {
                case INS_sve_brkpa:
                case INS_sve_brkpb:
                case INS_sve_brkn:
                    result.insLatency    = PERFSCORE_LATENCY_2C;
                    result.insThroughput = PERFSCORE_THROUGHPUT_1C;
                    break;

                case INS_sve_brkpas:
                case INS_sve_brkpbs:
                case INS_sve_brkns:
                    result.insLatency    = PERFSCORE_LATENCY_3C;
                    result.insThroughput = PERFSCORE_THROUGHPUT_1C;
                    break;

                default:
                    // all other instructions
                    perfScoreUnhandledInstruction(id, &result);
                    break;
            }
            break;

        case IF_SVE_DB_3A: // ................ ..gggg.NNNNMDDDD -- SVE partition break condition
            result.insLatency    = PERFSCORE_LATENCY_2C;
            result.insThroughput = PERFSCORE_THROUGHPUT_2C;
            break;

        case IF_SVE_DB_3B: // ................ ..gggg.NNNN.DDDD -- SVE partition break condition
            result.insLatency    = PERFSCORE_LATENCY_3C;
            result.insThroughput = PERFSCORE_THROUGHPUT_2C;
            break;

        case IF_SVE_DD_2A: // ................ .......gggg.DDDD -- SVE predicate first active
            result.insLatency    = PERFSCORE_LATENCY_3C;
            result.insThroughput = PERFSCORE_THROUGHPUT_2C;
            break;

        case IF_SVE_DE_1A: // ........xx...... ......ppppp.DDDD -- SVE predicate initialize
            switch (ins)
            {
                case INS_sve_ptrue:
                    result.insLatency    = PERFSCORE_LATENCY_2C;
                    result.insThroughput = PERFSCORE_THROUGHPUT_2C;
                    break;

                case INS_sve_ptrues:
                    result.insLatency    = PERFSCORE_LATENCY_3C;
                    result.insThroughput = PERFSCORE_THROUGHPUT_2C;
                    break;

                default:
                    // all other instructions
                    perfScoreUnhandledInstruction(id, &result);
                    break;
            }
            break;

        case IF_SVE_DF_2A: // ........xx...... .......VVVV.DDDD -- SVE predicate next active
            result.insLatency    = PERFSCORE_LATENCY_3C;
            result.insThroughput = PERFSCORE_THROUGHPUT_2C;
            break;

        case IF_SVE_DG_2A: // ................ .......gggg.DDDD -- SVE predicate read from FFR (predicated)
            switch (ins)
            {
                case INS_sve_rdffr:
                    result.insLatency    = PERFSCORE_LATENCY_3C;
                    result.insThroughput = PERFSCORE_THROUGHPUT_1C;
                    break;

                case INS_sve_rdffrs:
                    result.insLatency    = PERFSCORE_LATENCY_4C;
                    result.insThroughput = PERFSCORE_THROUGHPUT_2X;
                    break;

                default:
                    // all other instructions
                    perfScoreUnhandledInstruction(id, &result);
                    break;
            }
            break;

        case IF_SVE_DH_1A: // ................ ............DDDD -- SVE predicate read from FFR (unpredicated)
            result.insLatency    = PERFSCORE_LATENCY_3C;
            result.insThroughput = PERFSCORE_THROUGHPUT_1C;
            break;

        case IF_SVE_DJ_1A: // ................ ............DDDD -- SVE predicate zero
            result.insLatency    = PERFSCORE_LATENCY_2C;
            result.insThroughput = PERFSCORE_THROUGHPUT_2C;
            break;

        case IF_SVE_DI_2A: // ................ ..gggg.NNNN..... -- SVE predicate test
            result.insLatency    = PERFSCORE_LATENCY_1C;
            result.insThroughput = PERFSCORE_THROUGHPUT_2C;
            break;

        case IF_SVE_DK_3A: // ........xx...... ..gggg.NNNNddddd -- SVE predicate count
            result.insLatency    = PERFSCORE_LATENCY_2C;
            result.insThroughput = PERFSCORE_THROUGHPUT_2C;
            break;

        case IF_SVE_GE_4A: // ........xx.mmmmm ...gggnnnnn.DDDD -- SVE2 character match
        case IF_SVE_HT_4A: // ........xx.mmmmm ...gggnnnnn.DDDD -- SVE floating-point compare vectors
            result.insLatency    = PERFSCORE_LATENCY_2C;
            result.insThroughput = PERFSCORE_THROUGHPUT_1C;
            break;

        // Extract/insert operation, SIMD and FP scalar form
        case IF_SVE_CR_3A: // ........xx...... ...gggnnnnnddddd -- SVE extract element to SIMD&FP scalar register
            result.insLatency    = PERFSCORE_LATENCY_3C;
            result.insThroughput = PERFSCORE_THROUGHPUT_1C;
            break;

        // Extract/insert operation, scalar
        case IF_SVE_CS_3A: // ........xx...... ...gggnnnnnddddd -- SVE extract element to general register
            result.insLatency    = PERFSCORE_LATENCY_5C;
            result.insThroughput = PERFSCORE_THROUGHPUT_1C;
            break;

        // Count/reverse bits
        // Reverse, vector
        case IF_SVE_CU_3A: // ........xx...... ...gggnnnnnddddd -- SVE reverse within elements
            result.insLatency    = PERFSCORE_LATENCY_2C;
            result.insThroughput = PERFSCORE_THROUGHPUT_2X;
            break;

        case IF_SVE_ES_3A: // ........xx...... ...gggnnnnnddddd -- SVE2 integer unary operations (predicated)
            switch (ins)
            {
                // Arithmetic, complex
                case INS_sve_sqabs:
                case INS_sve_sqneg:
                    // Reciprocal estimate
                    result.insLatency    = PERFSCORE_LATENCY_2C;
                    result.insThroughput = PERFSCORE_THROUGHPUT_2C;
                    break;

                // Reciprocal estimate
                case INS_sve_urecpe:
                case INS_sve_ursqrte:
                    result.insLatency    = PERFSCORE_LATENCY_4C;
                    result.insThroughput = PERFSCORE_THROUGHPUT_2X;
                    break;

                default:
                    // all other instructions
                    perfScoreUnhandledInstruction(id, &result);
                    break;
            }
            break;

        // Arithmetic, pairwise add and accum long
        case IF_SVE_EQ_3A: // ........xx...... ...gggnnnnnddddd -- SVE2 integer pairwise add and accumulate long
        case IF_SVE_EF_3A: // ...........mmmmm ......nnnnnddddd -- SVE two-way dot product
            result.insLatency    = PERFSCORE_LATENCY_4C;
            result.insThroughput = PERFSCORE_THROUGHPUT_1C;
            break;

        case IF_SVE_GQ_3A: // ................ ...gggnnnnnddddd -- SVE floating-point convert precision odd elements
            result.insThroughput = PERFSCORE_THROUGHPUT_1C;
            result.insLatency    = PERFSCORE_LATENCY_3C;
            break;

        // Floating point arithmetic
        // Floating point min/max pairwise
        case IF_SVE_GR_3A: // ........xx...... ...gggmmmmmddddd -- SVE2 floating-point pairwise operations
            result.insLatency    = PERFSCORE_LATENCY_2C;
            result.insThroughput = PERFSCORE_THROUGHPUT_2X;
            break;

        // Floating point reduction, F64. (Note: Worse for F32 and F16)
        case IF_SVE_HE_3A: // ........xx...... ...gggnnnnnddddd -- SVE floating-point recursive reduction
            result.insLatency    = PERFSCORE_LATENCY_2C;
            result.insThroughput = PERFSCORE_THROUGHPUT_2C;
            break;

        // Floating point associative add, F64. (Note: Worse for F32 and F16)
        case IF_SVE_HJ_3A: // ........xx...... ...gggmmmmmddddd -- SVE floating-point serial reduction (predicated)
            result.insLatency    = PERFSCORE_LATENCY_4C;
            result.insThroughput = PERFSCORE_THROUGHPUT_2X;
            break;

        case IF_SVE_HL_3A: // ........xx...... ...gggmmmmmddddd -- SVE floating-point arithmetic (predicated)
            switch (ins)
            {
                // Floating point absolute value/difference
                case INS_sve_fabd:
                // Floating point min/max
                case INS_sve_fmax:
                case INS_sve_fmaxnm:
                case INS_sve_fmin:
                case INS_sve_fminnm:
                // Floating point arithmetic
                case INS_sve_fadd:
                case INS_sve_fsub:
                case INS_sve_fsubr:
                    result.insLatency    = PERFSCORE_LATENCY_2C;
                    result.insThroughput = PERFSCORE_THROUGHPUT_2X;
                    break;

                // Floating point divide, F64 (Note: Worse for F32, F16)
                case INS_sve_fdiv:
                case INS_sve_fdivr:
                    result.insLatency    = PERFSCORE_LATENCY_15C;    // 7 to 15
                    result.insThroughput = PERFSCORE_THROUGHPUT_14C; // 1/14 to 1/7
                    break;

                // Floating point multiply
                case INS_sve_fmul:
                case INS_sve_fmulx:
                case INS_sve_fscale:
                    result.insLatency    = PERFSCORE_LATENCY_3C;
                    result.insThroughput = PERFSCORE_THROUGHPUT_2X;
                    break;

                case INS_sve_famax:
                case INS_sve_famin:
                    result.insLatency    = PERFSCORE_LATENCY_20C;    // TODO-SVE: Placeholder
                    result.insThroughput = PERFSCORE_THROUGHPUT_25C; // TODO-SVE: Placeholder
                    break;

                default:
                    // all other instructions
                    perfScoreUnhandledInstruction(id, &result);
                    break;
            }
            break;

        case IF_SVE_HO_3A: // ................ ...gggnnnnnddddd -- SVE floating-point convert precision
        case IF_SVE_HO_3B:
        case IF_SVE_HO_3C:
        case IF_SVE_HP_3B: // ................ ...gggnnnnnddddd -- SVE floating-point convert to integer
            result.insThroughput = PERFSCORE_THROUGHPUT_1C;
            result.insLatency    = PERFSCORE_LATENCY_3C;
            break;

        // Floating point round to integral, F64. (Note: Worse for F32 and F16)
        case IF_SVE_HQ_3A: // ........xx...... ...gggnnnnnddddd -- SVE floating-point round to integral value
            result.insLatency    = PERFSCORE_LATENCY_3C;
            result.insThroughput = PERFSCORE_THROUGHPUT_1C;
            break;

        case IF_SVE_HR_3A: // ........xx...... ...gggnnnnnddddd -- SVE floating-point unary operations
            switch (ins)
            {
                // Floating point reciprocal estimate, F64. (Note: Worse for F32 and F16)
                case INS_sve_frecpx:
                    result.insThroughput = PERFSCORE_THROUGHPUT_3C;
                    result.insLatency    = PERFSCORE_LATENCY_1C;
                    break;

                // Floating point square root F64. (Note: Worse for F32 and F16)
                case INS_sve_fsqrt:
                    result.insThroughput = PERFSCORE_THROUGHPUT_16C;
                    result.insLatency    = PERFSCORE_LATENCY_14C;
                    break;

                default:
                    // all other instructions
                    perfScoreUnhandledInstruction(id, &result);
                    break;
            }
            break;

        case IF_SVE_HS_3A: // ................ ...gggnnnnnddddd -- SVE integer convert to floating-point
            result.insThroughput = PERFSCORE_THROUGHPUT_4X;
            result.insLatency    = PERFSCORE_LATENCY_6C;
            break;

        case IF_SVE_DL_2A: // ........xx...... .....l.NNNNddddd -- SVE predicate count (predicate-as-counter)
            result.insThroughput = PERFSCORE_THROUGHPUT_2C;
            result.insLatency    = PERFSCORE_LATENCY_2C;
            break;

        case IF_SVE_DM_2A: // ........xx...... .......MMMMddddd -- SVE inc/dec register by predicate count
        case IF_SVE_DN_2A: // ........xx...... .......MMMMddddd -- SVE inc/dec vector by predicate count
        case IF_SVE_DP_2A: // ........xx...... .......MMMMddddd -- SVE saturating inc/dec vector by predicate count
        case IF_SVE_DO_2A: // ........xx...... .....X.MMMMddddd -- SVE saturating inc/dec register by predicate count
            result.insThroughput = PERFSCORE_THROUGHPUT_1C;
            result.insLatency    = PERFSCORE_LATENCY_7C;
            break;

        case IF_SVE_DQ_0A: // ................ ................ -- SVE FFR initialise
        case IF_SVE_DR_1A: // ................ .......NNNN..... -- SVE FFR write from predicate
            result.insThroughput = PERFSCORE_THROUGHPUT_1C;
            result.insLatency    = PERFSCORE_LATENCY_2C;
            break;

        case IF_SVE_DW_2A: // ........xx...... ......iiNNN.DDDD -- SVE extract mask predicate from predicate-as-counter
        case IF_SVE_DW_2B: // ........xx...... .......iNNN.DDDD -- SVE extract mask predicate from predicate-as-counter
        case IF_SVE_DS_2A: // .........x.mmmmm ......nnnnn..... -- SVE conditionally terminate scalars
            result.insThroughput = PERFSCORE_THROUGHPUT_1C;
            result.insLatency    = PERFSCORE_LATENCY_1C;
            break;

        case IF_SVE_DV_4A:   // ........ix.xxxvv ..NNNN.MMMM.DDDD -- SVE broadcast predicate element
        case IF_SVE_FZ_2A:   // ................ ......nnnn.ddddd -- SME2 multi-vec extract narrow
        case IF_SVE_GY_3A:   // ...........iimmm ....i.nnnnnddddd -- SVE BFloat16 floating-point dot product (indexed)
        case IF_SVE_GY_3B_D: // ...........iimmm ......nnnnnddddd -- SVE BFloat16 floating-point dot product (indexed)
            result.insThroughput = PERFSCORE_THROUGHPUT_1C; // need to fix
            result.insLatency    = PERFSCORE_LATENCY_1C;    // need to fix
            break;

        case IF_SVE_HG_2A: // ................ ......nnnn.ddddd -- SVE2 FP8 downconverts
            switch (ins)
            {
                case INS_sve_fcvtnt:
                    result.insThroughput = PERFSCORE_THROUGHPUT_1C;
                    result.insLatency    = PERFSCORE_LATENCY_3C;
                    break;
                case INS_sve_fcvtn:
                case INS_sve_bfcvtn:
                case INS_sve_fcvtnb:
                    result.insThroughput = PERFSCORE_THROUGHPUT_1C; // need to fix
                    result.insLatency    = PERFSCORE_LATENCY_1C;    // need to fix
                    break;
                default:
                    // all other instructions
                    perfScoreUnhandledInstruction(id, &result);
                    break;
            }
            break;

        // Not available in Arm Neoverse N2 Software Optimization Guide.
        case IF_SVE_AG_3A: // ........xx...... ...gggnnnnnddddd -- SVE bitwise logical reduction (quadwords)
        case IF_SVE_AJ_3A: // ........xx...... ...gggnnnnnddddd -- SVE integer add reduction (quadwords)
        case IF_SVE_AL_3A: // ........xx...... ...gggnnnnnddddd -- SVE integer min/max reduction (quadwords)
        case IF_SVE_GS_3A: // ........xx...... ...gggnnnnnddddd -- SVE floating-point recursive reduction (quadwords)
            result.insLatency    = PERFSCORE_LATENCY_20C;    // TODO-SVE: Placeholder
            result.insThroughput = PERFSCORE_THROUGHPUT_25C; // TODO-SVE: Placeholder
            break;

        // Not available in Arm Neoverse N2 Software Optimization Guide.
        case IF_SVE_GA_2A: // ............iiii ......nnnn.ddddd -- SME2 multi-vec shift narrow
            result.insThroughput = PERFSCORE_THROUGHPUT_25C; // TODO-SVE: Placeholder
            result.insLatency    = PERFSCORE_LATENCY_20C;    // TODO-SVE: Placeholder
            break;

        case IF_SVE_GD_2A: // .........x.xx... ......nnnnnddddd -- SVE2 saturating extract narrow
        case IF_SVE_FA_3A: // ...........iimmm ....rrnnnnnddddd -- SVE2 complex integer dot product (indexed)
        case IF_SVE_FA_3B: // ...........immmm ....rrnnnnnddddd -- SVE2 complex integer dot product (indexed)
        case IF_SVE_EJ_3A: // ........xx.mmmmm ....rrnnnnnddddd -- SVE2 complex integer dot product
            result.insThroughput = PERFSCORE_THROUGHPUT_1C;
            result.insLatency    = PERFSCORE_LATENCY_4C;
            break;

        case IF_SVE_GK_2A: // ................ ......mmmmmddddd -- SVE2 crypto destructive binary operations
        case IF_SVE_GL_1A: // ................ ...........ddddd -- SVE2 crypto unary operations
            result.insThroughput = PERFSCORE_THROUGHPUT_2C;
            result.insLatency    = PERFSCORE_LATENCY_2C;
            break;

        case IF_SVE_DT_3A: // ........xx.mmmmm ...X..nnnnn.DDDD -- SVE integer compare scalar count and limit
        case IF_SVE_DX_3A: // ........xx.mmmmm ......nnnnn.DDD. -- SVE integer compare scalar count and limit (predicate
                           // pair)
        case IF_SVE_DY_3A: // ........xx.mmmmm ..l...nnnnn..DDD -- SVE integer compare scalar count and limit
                           // (predicate-as-counter)
        case IF_SVE_DU_3A: // ........xx.mmmmm ......nnnnn.DDDD -- SVE pointer conflict compare
            result.insThroughput = PERFSCORE_THROUGHPUT_1C;
            result.insLatency    = PERFSCORE_LATENCY_3C;
            break;

        case IF_SVE_DZ_1A: // ........xx...... .............DDD -- sve_int_pn_ptrue
        case IF_SVE_EA_1A: // ........xx...... ...iiiiiiiiddddd -- SVE broadcast floating-point immediate (unpredicated)
        case IF_SVE_EB_1A: // ........xx...... ..hiiiiiiiiddddd -- SVE broadcast integer immediate (unpredicated)
        case IF_SVE_EC_1A: // ........xx...... ..hiiiiiiiiddddd -- SVE integer add/subtract immediate (unpredicated)
        case IF_SVE_EB_1B: // ........xx...... ...........ddddd -- SVE broadcast integer immediate (unpredicated)
        case IF_SVE_FV_2A: // ........xx...... .....rmmmmmddddd -- SVE2 complex integer add
        case IF_SVE_FY_3A: // .........x.mmmmm ......nnnnnddddd -- SVE2 integer add/subtract long with carry
            result.insThroughput = PERFSCORE_THROUGHPUT_2C;
            result.insLatency    = PERFSCORE_LATENCY_2C;
            break;

        case IF_SVE_ED_1A: // ........xx...... ...iiiiiiiiddddd -- SVE integer min/max immediate (unpredicated)
            switch (ins)
            {
                case INS_sve_umin:
                    result.insThroughput = PERFSCORE_THROUGHPUT_1C; // need to fix
                    result.insLatency    = PERFSCORE_LATENCY_1C;    // need to fix
                    break;
                default:
                    result.insThroughput = PERFSCORE_THROUGHPUT_2C;
                    result.insLatency    = PERFSCORE_LATENCY_2C;
                    break;
            }
            break;

        case IF_SVE_EE_1A: // ........xx...... ...iiiiiiiiddddd -- SVE integer multiply immediate (unpredicated)
        case IF_SVE_FB_3A: // ...........iimmm ....rrnnnnnddddd -- SVE2 complex integer multiply-add (indexed)
        case IF_SVE_FB_3B: // ...........immmm ....rrnnnnnddddd -- SVE2 complex integer multiply-add (indexed)
        case IF_SVE_FC_3A: // ...........iimmm ....rrnnnnnddddd -- SVE2 complex saturating multiply-add (indexed)
        case IF_SVE_FC_3B: // ...........immmm ....rrnnnnnddddd -- SVE2 complex saturating multiply-add (indexed)
        case IF_SVE_EK_3A: // ........xx.mmmmm ....rrnnnnnddddd -- SVE2 complex integer multiply-add
            result.insThroughput = PERFSCORE_THROUGHPUT_2X;
            result.insLatency    = PERFSCORE_LATENCY_5C;
            break;

        case IF_SVE_IH_3A:   // ............iiii ...gggnnnnnttttt -- SVE contiguous load (quadwords, scalar plus
                             // immediate)
        case IF_SVE_IH_3A_A: // ............iiii ...gggnnnnnttttt -- SVE contiguous load (quadwords, scalar plus
                             // immediate)
        case IF_SVE_IH_3A_F: // ............iiii ...gggnnnnnttttt -- SVE contiguous load (quadwords, scalar plus
                             // immediate)
        case IF_SVE_IJ_3A:   // ............iiii ...gggnnnnnttttt -- SVE contiguous load (scalar plus immediate)
        case IF_SVE_IJ_3A_D: // ............iiii ...gggnnnnnttttt -- SVE contiguous load (scalar plus immediate)
        case IF_SVE_IJ_3A_E: // ............iiii ...gggnnnnnttttt -- SVE contiguous load (scalar plus immediate)
        case IF_SVE_IJ_3A_F: // ............iiii ...gggnnnnnttttt -- SVE contiguous load (scalar plus immediate)
        case IF_SVE_IJ_3A_G: // ............iiii ...gggnnnnnttttt -- SVE contiguous load (scalar plus immediate)
            result.insThroughput = PERFSCORE_THROUGHPUT_1C;
            result.insLatency    = PERFSCORE_LATENCY_9C;
            break;

        case IF_SVE_IL_3A: // ............iiii ...gggnnnnnttttt -- SVE contiguous non-fault load (scalar plus immediate)
        case IF_SVE_IL_3A_A: // ............iiii ...gggnnnnnttttt -- SVE contiguous non-fault load (scalar plus
                             // immediate)
        case IF_SVE_IL_3A_B: // ............iiii ...gggnnnnnttttt -- SVE contiguous non-fault load (scalar plus
                             // immediate)
        case IF_SVE_IL_3A_C: // ............iiii ...gggnnnnnttttt -- SVE contiguous non-fault load (scalar plus
                             // immediate)
            result.insThroughput = PERFSCORE_THROUGHPUT_3C;
            result.insLatency    = PERFSCORE_LATENCY_6C;
            break;

        case IF_SVE_IM_3A: // ............iiii ...gggnnnnnttttt -- SVE contiguous non-temporal load (scalar plus
                           // immediate)
            result.insThroughput = PERFSCORE_THROUGHPUT_2X;
            result.insLatency    = PERFSCORE_LATENCY_10C;
            break;

        case IF_SVE_IO_3A: // ............iiii ...gggnnnnnttttt -- SVE load and broadcast quadword (scalar plus
                           // immediate)
            switch (ins)
            {
                case INS_sve_ld1rqb:
                    result.insThroughput = PERFSCORE_THROUGHPUT_3C;
                    result.insLatency    = PERFSCORE_LATENCY_6C;
                    break;
                case INS_sve_ld1rob:
                    result.insThroughput = PERFSCORE_THROUGHPUT_1C; // need to fix
                    result.insLatency    = PERFSCORE_LATENCY_1C;    // need to fix
                    break;
                case INS_sve_ld1rqh:
                    result.insThroughput = PERFSCORE_THROUGHPUT_3C;
                    result.insLatency    = PERFSCORE_LATENCY_6C;
                    break;
                case INS_sve_ld1roh:
                    result.insThroughput = PERFSCORE_THROUGHPUT_1C; // need to fix
                    result.insLatency    = PERFSCORE_LATENCY_1C;    // need to fix
                    break;
                case INS_sve_ld1rqw:
                    result.insThroughput = PERFSCORE_THROUGHPUT_3C;
                    result.insLatency    = PERFSCORE_LATENCY_6C;
                    break;
                case INS_sve_ld1row:
                    result.insThroughput = PERFSCORE_THROUGHPUT_1C; // need to fix
                    result.insLatency    = PERFSCORE_LATENCY_1C;    // need to fix
                    break;
                case INS_sve_ld1rqd:
                    result.insThroughput = PERFSCORE_THROUGHPUT_3C;
                    result.insLatency    = PERFSCORE_LATENCY_6C;
                    break;
                case INS_sve_ld1rod:
                    result.insThroughput = PERFSCORE_THROUGHPUT_1C; // need to fix
                    result.insLatency    = PERFSCORE_LATENCY_1C;    // need to fix
                    break;
                default:
                    // all other instructions
                    perfScoreUnhandledInstruction(id, &result);
                    break;
            }
            break;

        case IF_SVE_IQ_3A: // ............iiii ...gggnnnnnttttt -- SVE load multiple structures (quadwords, scalar plus
                           // immediate)
            switch (ins)
            {
                case INS_sve_ld2q:
                    result.insThroughput = PERFSCORE_THROUGHPUT_1C; // need to fix
                    result.insLatency    = PERFSCORE_LATENCY_1C;    // need to fix
                    break;
                case INS_sve_ld3q:
                    result.insThroughput = PERFSCORE_THROUGHPUT_1C; // need to fix
                    result.insLatency    = PERFSCORE_LATENCY_1C;    // need to fix
                    break;
                case INS_sve_ld4q:
                    result.insThroughput = PERFSCORE_THROUGHPUT_1C; // need to fix
                    result.insLatency    = PERFSCORE_LATENCY_1C;    // need to fix
                    break;
                default:
                    // all other instructions
                    perfScoreUnhandledInstruction(id, &result);
                    break;
            }
            break;

        case IF_SVE_IS_3A: // ............iiii ...gggnnnnnttttt -- SVE load multiple structures (scalar plus immediate)
            switch (ins)
            {
                case INS_sve_ld2b:
                    result.insThroughput = PERFSCORE_THROUGHPUT_1C;
                    result.insLatency    = PERFSCORE_LATENCY_9C;
                    break;
                case INS_sve_ld3b:
                    result.insThroughput = PERFSCORE_THROUGHPUT_3C;
                    result.insLatency    = PERFSCORE_LATENCY_10C;
                    break;
                case INS_sve_ld4b:
                    result.insThroughput = PERFSCORE_THROUGHPUT_2X;
                    result.insLatency    = PERFSCORE_LATENCY_10C;
                    break;
                case INS_sve_ld2h:
                    result.insThroughput = PERFSCORE_THROUGHPUT_1C;
                    result.insLatency    = PERFSCORE_LATENCY_9C;
                    break;
                case INS_sve_ld3h:
                    result.insThroughput = PERFSCORE_THROUGHPUT_3C;
                    result.insLatency    = PERFSCORE_LATENCY_10C;
                    break;
                case INS_sve_ld4h:
                    result.insThroughput = PERFSCORE_THROUGHPUT_2X;
                    result.insLatency    = PERFSCORE_LATENCY_10C;
                    break;
                case INS_sve_ld2w:
                    result.insThroughput = PERFSCORE_THROUGHPUT_1C;
                    result.insLatency    = PERFSCORE_LATENCY_9C;
                    break;
                case INS_sve_ld3w:
                    result.insThroughput = PERFSCORE_THROUGHPUT_3C;
                    result.insLatency    = PERFSCORE_LATENCY_10C;
                    break;
                case INS_sve_ld4w:
                    result.insThroughput = PERFSCORE_THROUGHPUT_2X;
                    result.insLatency    = PERFSCORE_LATENCY_10C;
                    break;
                case INS_sve_ld2d:
                    result.insThroughput = PERFSCORE_THROUGHPUT_1C;
                    result.insLatency    = PERFSCORE_LATENCY_9C;
                    break;
                case INS_sve_ld3d:
                    result.insThroughput = PERFSCORE_THROUGHPUT_3C;
                    result.insLatency    = PERFSCORE_LATENCY_10C;
                    break;
                case INS_sve_ld4d:
                    result.insThroughput = PERFSCORE_THROUGHPUT_2X;
                    result.insLatency    = PERFSCORE_LATENCY_10C;
                    break;
                default:
                    // all other instructions
                    perfScoreUnhandledInstruction(id, &result);
                    break;
            }
            break;

        case IF_SVE_JE_3A: // ............iiii ...gggnnnnnttttt -- SVE store multiple structures (quadwords, scalar plus
                           // immediate)
            switch (ins)
            {
                case INS_sve_st2q:
                    result.insThroughput = PERFSCORE_THROUGHPUT_1C; // need to fix
                    result.insLatency    = PERFSCORE_LATENCY_1C;    // need to fix
                    break;
                case INS_sve_st3q:
                    result.insThroughput = PERFSCORE_THROUGHPUT_1C; // need to fix
                    result.insLatency    = PERFSCORE_LATENCY_1C;    // need to fix
                    break;
                case INS_sve_st4q:
                    result.insThroughput = PERFSCORE_THROUGHPUT_1C; // need to fix
                    result.insLatency    = PERFSCORE_LATENCY_1C;    // need to fix
                    break;
                default:
                    // all other instructions
                    perfScoreUnhandledInstruction(id, &result);
                    break;
            }
            break;

        case IF_SVE_FR_2A:   // .........x.xxiii ......nnnnnddddd -- SVE2 bitwise shift left long
        case IF_SVE_JM_3A:   // ............iiii ...gggnnnnnttttt -- SVE contiguous non-temporal store (scalar plus
                             // immediate)
        case IF_SVE_JN_3C:   // ............iiii ...gggnnnnnttttt -- SVE contiguous store (scalar plus immediate)
        case IF_SVE_JN_3C_D: // ............iiii ...gggnnnnnttttt -- SVE contiguous store (scalar plus immediate)
            result.insThroughput = PERFSCORE_THROUGHPUT_1C;
            result.insLatency    = PERFSCORE_LATENCY_2C;
            break;

        case IF_SVE_GB_2A: // .........x.xxiii ......nnnnnddddd -- SVE2 bitwise shift right narrow
            switch (ins)
            {
                case INS_sve_sqshrunb:
                    result.insThroughput = PERFSCORE_THROUGHPUT_1C;
                    result.insLatency    = PERFSCORE_LATENCY_4C;
                    break;
                case INS_sve_sqshrunt:
                    result.insThroughput = PERFSCORE_THROUGHPUT_1C;
                    result.insLatency    = PERFSCORE_LATENCY_4C;
                    break;
                case INS_sve_sqrshrunb:
                    result.insThroughput = PERFSCORE_THROUGHPUT_1C;
                    result.insLatency    = PERFSCORE_LATENCY_4C;
                    break;
                case INS_sve_sqrshrunt:
                    result.insThroughput = PERFSCORE_THROUGHPUT_1C;
                    result.insLatency    = PERFSCORE_LATENCY_4C;
                    break;
                case INS_sve_shrnb:
                    result.insThroughput = PERFSCORE_THROUGHPUT_1C;
                    result.insLatency    = PERFSCORE_LATENCY_2C;
                    break;
                case INS_sve_shrnt:
                    result.insThroughput = PERFSCORE_THROUGHPUT_1C;
                    result.insLatency    = PERFSCORE_LATENCY_2C;
                    break;
                case INS_sve_rshrnb:
                    result.insThroughput = PERFSCORE_THROUGHPUT_1C;
                    result.insLatency    = PERFSCORE_LATENCY_4C;
                    break;
                case INS_sve_rshrnt:
                    result.insThroughput = PERFSCORE_THROUGHPUT_1C;
                    result.insLatency    = PERFSCORE_LATENCY_4C;
                    break;
                case INS_sve_sqshrnb:
                    result.insThroughput = PERFSCORE_THROUGHPUT_1C;
                    result.insLatency    = PERFSCORE_LATENCY_4C;
                    break;
                case INS_sve_sqshrnt:
                    result.insThroughput = PERFSCORE_THROUGHPUT_1C;
                    result.insLatency    = PERFSCORE_LATENCY_4C;
                    break;
                case INS_sve_sqrshrnb:
                    result.insThroughput = PERFSCORE_THROUGHPUT_1C;
                    result.insLatency    = PERFSCORE_LATENCY_4C;
                    break;
                case INS_sve_sqrshrnt:
                    result.insThroughput = PERFSCORE_THROUGHPUT_1C;
                    result.insLatency    = PERFSCORE_LATENCY_4C;
                    break;
                case INS_sve_uqshrnb:
                    result.insThroughput = PERFSCORE_THROUGHPUT_1C;
                    result.insLatency    = PERFSCORE_LATENCY_4C;
                    break;
                case INS_sve_uqshrnt:
                    result.insThroughput = PERFSCORE_THROUGHPUT_1C;
                    result.insLatency    = PERFSCORE_LATENCY_4C;
                    break;
                case INS_sve_uqrshrnb:
                    result.insThroughput = PERFSCORE_THROUGHPUT_1C;
                    result.insLatency    = PERFSCORE_LATENCY_4C;
                    break;
                case INS_sve_uqrshrnt:
                    result.insThroughput = PERFSCORE_THROUGHPUT_1C;
                    result.insLatency    = PERFSCORE_LATENCY_4C;
                    break;
                default:
                    // all other instructions
                    perfScoreUnhandledInstruction(id, &result);
                    break;
            }
            break;

        case IF_SVE_JO_3A: // ............iiii ...gggnnnnnttttt -- SVE store multiple structures (scalar plus immediate)
            switch (ins)
            {
                case INS_sve_st2b:
                    result.insThroughput = PERFSCORE_THROUGHPUT_1C;
                    result.insLatency    = PERFSCORE_LATENCY_4C;
                    break;
                case INS_sve_st3b:
                    result.insThroughput = PERFSCORE_THROUGHPUT_9C;
                    result.insLatency    = PERFSCORE_LATENCY_7C;
                    break;
                case INS_sve_st4b:
                    result.insThroughput = PERFSCORE_THROUGHPUT_9C;
                    result.insLatency    = PERFSCORE_LATENCY_11C;
                    break;
                case INS_sve_st2h:
                    result.insThroughput = PERFSCORE_THROUGHPUT_1C;
                    result.insLatency    = PERFSCORE_LATENCY_4C;
                    break;
                case INS_sve_st3h:
                    result.insThroughput = PERFSCORE_THROUGHPUT_9C;
                    result.insLatency    = PERFSCORE_LATENCY_7C;
                    break;
                case INS_sve_st4h:
                    result.insThroughput = PERFSCORE_THROUGHPUT_9C;
                    result.insLatency    = PERFSCORE_LATENCY_11C;
                    break;
                case INS_sve_st2w:
                    result.insThroughput = PERFSCORE_THROUGHPUT_1C;
                    result.insLatency    = PERFSCORE_LATENCY_4C;
                    break;
                case INS_sve_st3w:
                    result.insThroughput = PERFSCORE_THROUGHPUT_9C;
                    result.insLatency    = PERFSCORE_LATENCY_7C;
                    break;
                case INS_sve_st4w:
                    result.insThroughput = PERFSCORE_THROUGHPUT_9C;
                    result.insLatency    = PERFSCORE_LATENCY_11C;
                    break;
                case INS_sve_st2d:
                    result.insThroughput = PERFSCORE_THROUGHPUT_1C;
                    result.insLatency    = PERFSCORE_LATENCY_4C;
                    break;
                case INS_sve_st3d:
                    result.insThroughput = PERFSCORE_THROUGHPUT_9C;
                    result.insLatency    = PERFSCORE_LATENCY_7C;
                    break;
                case INS_sve_st4d:
                    result.insThroughput = PERFSCORE_THROUGHPUT_9C;
                    result.insLatency    = PERFSCORE_LATENCY_11C;
                    break;
                default:
                    // all other instructions
                    perfScoreUnhandledInstruction(id, &result);
                    break;
            }
            break;

        case IF_SVE_JD_4A:   // .........xxmmmmm ...gggnnnnnttttt -- SVE contiguous store (scalar plus scalar)
        case IF_SVE_JD_4B:   // ..........xmmmmm ...gggnnnnnttttt -- SVE contiguous store (scalar plus scalar)
        case IF_SVE_JJ_4A:   // ...........mmmmm .h.gggnnnnnttttt -- SVE 64-bit scatter store (scalar plus 64-bit scaled
                             // offsets)
        case IF_SVE_JJ_4A_B: // ...........mmmmm .h.gggnnnnnttttt -- SVE 64-bit scatter store (scalar plus 64-bit scaled
                             // offsets)
        case IF_SVE_JJ_4A_C: // ...........mmmmm .h.gggnnnnnttttt -- SVE 64-bit scatter store (scalar plus 64-bit scaled
                             // offsets)
        case IF_SVE_JJ_4A_D: // ...........mmmmm .h.gggnnnnnttttt -- SVE 64-bit scatter store (scalar plus 64-bit scaled
                             // offsets)
        case IF_SVE_JK_4A: // ...........mmmmm .h.gggnnnnnttttt -- SVE 64-bit scatter store (scalar plus 64-bit unscaled
                           // offsets)
        case IF_SVE_JK_4A_B: // ...........mmmmm .h.gggnnnnnttttt -- SVE 64-bit scatter store (scalar plus 64-bit
                             // unscaled offsets)
        case IF_SVE_JN_3A:   // .........xx.iiii ...gggnnnnnttttt -- SVE contiguous store (scalar plus immediate)
        case IF_SVE_JN_3B:   // ..........x.iiii ...gggnnnnnttttt -- SVE contiguous store (scalar plus immediate)
            result.insThroughput = PERFSCORE_THROUGHPUT_1C;
            result.insLatency    = PERFSCORE_LATENCY_2C;
            break;

        case IF_SVE_HW_4A:   // .........h.mmmmm ...gggnnnnnttttt -- SVE 32-bit gather load (scalar plus 32-bit unscaled
                             // offsets)
        case IF_SVE_HW_4A_A: // .........h.mmmmm ...gggnnnnnttttt -- SVE 32-bit gather load (scalar plus 32-bit unscaled
                             // offsets)
        case IF_SVE_HW_4A_B: // .........h.mmmmm ...gggnnnnnttttt -- SVE 32-bit gather load (scalar plus 32-bit unscaled
                             // offsets)
        case IF_SVE_HW_4A_C: // .........h.mmmmm ...gggnnnnnttttt -- SVE 32-bit gather load (scalar plus 32-bit unscaled
                             // offsets)
        case IF_SVE_IU_4A:   // .........h.mmmmm ...gggnnnnnttttt -- SVE 64-bit gather load (scalar plus 32-bit unpacked
                             // scaled offsets)
        case IF_SVE_IU_4A_A: // .........h.mmmmm ...gggnnnnnttttt -- SVE 64-bit gather load (scalar plus 32-bit unpacked
                             // scaled offsets)
        case IF_SVE_IU_4A_C: // .........h.mmmmm ...gggnnnnnttttt -- SVE 64-bit gather load (scalar plus 32-bit unpacked
                             // scaled offsets)
        case IF_SVE_HW_4B:   // ...........mmmmm ...gggnnnnnttttt -- SVE 32-bit gather load (scalar plus 32-bit unscaled
                             // offsets)
        case IF_SVE_HW_4B_D: // ...........mmmmm ...gggnnnnnttttt -- SVE 32-bit gather load (scalar plus 32-bit unscaled
                             // offsets)
            result.insThroughput = PERFSCORE_THROUGHPUT_1C;
            result.insLatency    = PERFSCORE_LATENCY_9C;
            break;

        case IF_SVE_IF_4A:   // ...........mmmmm ...gggnnnnnttttt -- SVE2 32-bit gather non-temporal load (vector plus
                             // scalar)
        case IF_SVE_IF_4A_A: // ...........mmmmm ...gggnnnnnttttt -- SVE2 32-bit gather non-temporal load (vector plus
                             // scalar)
            result.insThroughput = PERFSCORE_THROUGHPUT_2X;
            result.insLatency    = PERFSCORE_LATENCY_10C;
            break;

        case IF_SVE_IG_4A: // ...........mmmmm ...gggnnnnnttttt -- SVE contiguous first-fault load (scalar plus scalar)
        case IF_SVE_IG_4A_D: // ...........mmmmm ...gggnnnnnttttt -- SVE contiguous first-fault load (scalar plus
                             // scalar)
        case IF_SVE_IG_4A_E: // ...........mmmmm ...gggnnnnnttttt -- SVE contiguous first-fault load (scalar plus
                             // scalar)
        case IF_SVE_IG_4A_F: // ...........mmmmm ...gggnnnnnttttt -- SVE contiguous first-fault load (scalar plus
                             // scalar)
        case IF_SVE_IG_4A_G: // ...........mmmmm ...gggnnnnnttttt -- SVE contiguous first-fault load (scalar plus
                             // scalar)
        case IF_SVE_II_4A:   // ...........mmmmm ...gggnnnnnttttt -- SVE contiguous load (quadwords, scalar plus scalar)
        case IF_SVE_II_4A_B: // ...........mmmmm ...gggnnnnnttttt -- SVE contiguous load (quadwords, scalar plus scalar)
        case IF_SVE_II_4A_H: // ...........mmmmm ...gggnnnnnttttt -- SVE contiguous load (quadwords, scalar plus scalar)
        case IF_SVE_IK_4A:   // ...........mmmmm ...gggnnnnnttttt -- SVE contiguous load (scalar plus scalar)
        case IF_SVE_IK_4A_F: // ...........mmmmm ...gggnnnnnttttt -- SVE contiguous load (scalar plus scalar)
        case IF_SVE_IK_4A_G: // ...........mmmmm ...gggnnnnnttttt -- SVE contiguous load (scalar plus scalar)
        case IF_SVE_IK_4A_H: // ...........mmmmm ...gggnnnnnttttt -- SVE contiguous load (scalar plus scalar)
        case IF_SVE_IK_4A_I: // ...........mmmmm ...gggnnnnnttttt -- SVE contiguous load (scalar plus scalar)
            result.insThroughput = PERFSCORE_THROUGHPUT_1C;
            result.insLatency    = PERFSCORE_LATENCY_9C;
            break;

        case IF_SVE_IN_4A: // ...........mmmmm ...gggnnnnnttttt -- SVE contiguous non-temporal load (scalar plus scalar)
            result.insThroughput = PERFSCORE_THROUGHPUT_2X;
            result.insLatency    = PERFSCORE_LATENCY_10C;
            break;

        case IF_SVE_IP_4A: // ...........mmmmm ...gggnnnnnttttt -- SVE load and broadcast quadword (scalar plus scalar)
            switch (ins)
            {
                case INS_sve_ld1rqb:
                    result.insThroughput = PERFSCORE_THROUGHPUT_3C;
                    result.insLatency    = PERFSCORE_LATENCY_6C;
                    break;
                case INS_sve_ld1rob:
                    result.insThroughput = PERFSCORE_THROUGHPUT_1C; // need to fix
                    result.insLatency    = PERFSCORE_LATENCY_1C;    // need to fix
                    break;
                case INS_sve_ld1rqh:
                    result.insThroughput = PERFSCORE_THROUGHPUT_3C;
                    result.insLatency    = PERFSCORE_LATENCY_6C;
                    break;
                case INS_sve_ld1roh:
                    result.insThroughput = PERFSCORE_THROUGHPUT_1C; // need to fix
                    result.insLatency    = PERFSCORE_LATENCY_1C;    // need to fix
                    break;
                case INS_sve_ld1rqw:
                    result.insThroughput = PERFSCORE_THROUGHPUT_3C;
                    result.insLatency    = PERFSCORE_LATENCY_6C;
                    break;
                case INS_sve_ld1row:
                    result.insThroughput = PERFSCORE_THROUGHPUT_1C; // need to fix
                    result.insLatency    = PERFSCORE_LATENCY_1C;    // need to fix
                    break;
                case INS_sve_ld1rqd:
                    result.insThroughput = PERFSCORE_THROUGHPUT_3C;
                    result.insLatency    = PERFSCORE_LATENCY_6C;
                    break;
                case INS_sve_ld1rod:
                    result.insThroughput = PERFSCORE_THROUGHPUT_1C; // need to fix
                    result.insLatency    = PERFSCORE_LATENCY_1C;    // need to fix
                    break;
                default:
                    // all other instructions
                    perfScoreUnhandledInstruction(id, &result);
                    break;
            }
            break;

        case IF_SVE_IR_4A: // ...........mmmmm ...gggnnnnnttttt -- SVE load multiple structures (quadwords, scalar plus
                           // scalar)
            switch (ins)
            {
                case INS_sve_ld2q:
                    result.insThroughput = PERFSCORE_THROUGHPUT_1C; // need to fix
                    result.insLatency    = PERFSCORE_LATENCY_1C;    // need to fix
                    break;
                case INS_sve_ld3q:
                    result.insThroughput = PERFSCORE_THROUGHPUT_1C; // need to fix
                    result.insLatency    = PERFSCORE_LATENCY_1C;    // need to fix
                    break;
                case INS_sve_ld4q:
                    result.insThroughput = PERFSCORE_THROUGHPUT_1C; // need to fix
                    result.insLatency    = PERFSCORE_LATENCY_1C;    // need to fix
                    break;
                default:
                    // all other instructions
                    perfScoreUnhandledInstruction(id, &result);
                    break;
            }
            break;

        case IF_SVE_IT_4A: // ...........mmmmm ...gggnnnnnttttt -- SVE load multiple structures (scalar plus scalar)
            switch (ins)
            {
                case INS_sve_ld2b:
                    result.insThroughput = PERFSCORE_THROUGHPUT_1C;
                    result.insLatency    = PERFSCORE_LATENCY_9C;
                    break;
                case INS_sve_ld3b:
                    result.insThroughput = PERFSCORE_THROUGHPUT_2X;
                    result.insLatency    = PERFSCORE_LATENCY_10C;
                    break;
                case INS_sve_ld4b:
                    result.insThroughput = PERFSCORE_THROUGHPUT_2X;
                    result.insLatency    = PERFSCORE_LATENCY_10C;
                    break;
                case INS_sve_ld2h:
                    result.insThroughput = PERFSCORE_THROUGHPUT_1C;
                    result.insLatency    = PERFSCORE_LATENCY_9C;
                    break;
                case INS_sve_ld3h:
                    result.insThroughput = PERFSCORE_THROUGHPUT_2X;
                    result.insLatency    = PERFSCORE_LATENCY_10C;
                    break;
                case INS_sve_ld4h:
                    result.insThroughput = PERFSCORE_THROUGHPUT_2X;
                    result.insLatency    = PERFSCORE_LATENCY_10C;
                    break;
                case INS_sve_ld2w:
                    result.insThroughput = PERFSCORE_THROUGHPUT_1C;
                    result.insLatency    = PERFSCORE_LATENCY_9C;
                    break;
                case INS_sve_ld3w:
                    result.insThroughput = PERFSCORE_THROUGHPUT_2X;
                    result.insLatency    = PERFSCORE_LATENCY_10C;
                    break;
                case INS_sve_ld4w:
                    result.insThroughput = PERFSCORE_THROUGHPUT_2X;
                    result.insLatency    = PERFSCORE_LATENCY_10C;
                    break;
                case INS_sve_ld2d:
                    result.insThroughput = PERFSCORE_THROUGHPUT_1C;
                    result.insLatency    = PERFSCORE_LATENCY_9C;
                    break;
                case INS_sve_ld3d:
                    result.insThroughput = PERFSCORE_THROUGHPUT_2X;
                    result.insLatency    = PERFSCORE_LATENCY_10C;
                    break;
                case INS_sve_ld4d:
                    result.insThroughput = PERFSCORE_THROUGHPUT_2X;
                    result.insLatency    = PERFSCORE_LATENCY_10C;
                    break;
                default:
                    // all other instructions
                    perfScoreUnhandledInstruction(id, &result);
                    break;
            }
            break;

        case IF_SVE_IU_4B:   // ...........mmmmm ...gggnnnnnttttt -- SVE 64-bit gather load (scalar plus 32-bit unpacked
                             // scaled offsets)
        case IF_SVE_IU_4B_B: // ...........mmmmm ...gggnnnnnttttt -- SVE 64-bit gather load (scalar plus 32-bit unpacked
                             // scaled offsets)
        case IF_SVE_IU_4B_D: // ...........mmmmm ...gggnnnnnttttt -- SVE 64-bit gather load (scalar plus 32-bit unpacked
                             // scaled offsets)
            result.insThroughput = PERFSCORE_THROUGHPUT_1C;
            result.insLatency    = PERFSCORE_LATENCY_9C;
            break;

        case IF_SVE_IW_4A: // ...........mmmmm ...gggnnnnnttttt -- SVE2 128-bit gather load (vector plus scalar)
            switch (ins)
            {
                case INS_sve_ld1q:
                    result.insThroughput = PERFSCORE_THROUGHPUT_1C; // need to fix
                    result.insLatency    = PERFSCORE_LATENCY_1C;    // need to fix
                    break;
                default:
                    // all other instructions
                    perfScoreUnhandledInstruction(id, &result);
                    break;
            }
            break;

        case IF_SVE_IX_4A: // ...........mmmmm ...gggnnnnnttttt -- SVE2 64-bit gather non-temporal load (vector plus
                           // scalar)
            result.insThroughput = PERFSCORE_THROUGHPUT_2X;
            result.insLatency    = PERFSCORE_LATENCY_10C;
            break;

        case IF_SVE_IY_4A: // ...........mmmmm ...gggnnnnnttttt -- SVE2 128-bit scatter store (vector plus scalar)
            switch (ins)
            {
                case INS_sve_st1q:
                    result.insThroughput = PERFSCORE_THROUGHPUT_1C; // need to fix
                    result.insLatency    = PERFSCORE_LATENCY_1C;    // need to fix
                    break;
                default:
                    // all other instructions
                    perfScoreUnhandledInstruction(id, &result);
                    break;
            }
            break;

        case IF_SVE_IZ_4A:   // ...........mmmmm ...gggnnnnnttttt -- SVE2 32-bit scatter non-temporal store (vector plus
                             // scalar)
        case IF_SVE_IZ_4A_A: // ...........mmmmm ...gggnnnnnttttt -- SVE2 32-bit scatter non-temporal store (vector plus
                             // scalar)
        case IF_SVE_JA_4A:   // ...........mmmmm ...gggnnnnnttttt -- SVE2 64-bit scatter non-temporal store (vector plus
                             // scalar)
        case IF_SVE_JB_4A:   // ...........mmmmm ...gggnnnnnttttt -- SVE contiguous non-temporal store (scalar plus
                             // scalar)
        case IF_SVE_JD_4C:   // ...........mmmmm ...gggnnnnnttttt -- SVE contiguous store (scalar plus scalar)
        case IF_SVE_JD_4C_A: // ...........mmmmm ...gggnnnnnttttt -- SVE contiguous store (scalar plus scalar)
            result.insThroughput = PERFSCORE_THROUGHPUT_1C;
            result.insLatency    = PERFSCORE_LATENCY_2C;
            break;

        case IF_SVE_JC_4A: // ...........mmmmm ...gggnnnnnttttt -- SVE store multiple structures (scalar plus scalar)
            switch (ins)
            {
                case INS_sve_st2b:
                    result.insThroughput = PERFSCORE_THROUGHPUT_1C;
                    result.insLatency    = PERFSCORE_LATENCY_4C;
                    break;
                case INS_sve_st3b:
                    result.insThroughput = PERFSCORE_THROUGHPUT_2X;
                    result.insLatency    = PERFSCORE_LATENCY_7C;
                    break;
                case INS_sve_st4b:
                    result.insThroughput = PERFSCORE_THROUGHPUT_9X;
                    result.insLatency    = PERFSCORE_LATENCY_11C;
                    break;
                case INS_sve_st2h:
                    result.insThroughput = PERFSCORE_THROUGHPUT_1C;
                    result.insLatency    = PERFSCORE_LATENCY_4C;
                    break;
                case INS_sve_st3h:
                    result.insThroughput = PERFSCORE_THROUGHPUT_2X;
                    result.insLatency    = PERFSCORE_LATENCY_7C;
                    break;
                case INS_sve_st4h:
                    result.insThroughput = PERFSCORE_THROUGHPUT_9X;
                    result.insLatency    = PERFSCORE_LATENCY_11C;
                    break;
                case INS_sve_st2w:
                    result.insThroughput = PERFSCORE_THROUGHPUT_1C;
                    result.insLatency    = PERFSCORE_LATENCY_4C;
                    break;
                case INS_sve_st3w:
                    result.insThroughput = PERFSCORE_THROUGHPUT_2X;
                    result.insLatency    = PERFSCORE_LATENCY_7C;
                    break;
                case INS_sve_st4w:
                    result.insThroughput = PERFSCORE_THROUGHPUT_9X;
                    result.insLatency    = PERFSCORE_LATENCY_11C;
                    break;
                case INS_sve_st2d:
                    result.insThroughput = PERFSCORE_THROUGHPUT_1C;
                    result.insLatency    = PERFSCORE_LATENCY_4C;
                    break;
                case INS_sve_st3d:
                    result.insThroughput = PERFSCORE_THROUGHPUT_2X;
                    result.insLatency    = PERFSCORE_LATENCY_7C;
                    break;
                case INS_sve_st4d:
                    result.insThroughput = PERFSCORE_THROUGHPUT_9X;
                    result.insLatency    = PERFSCORE_LATENCY_11C;
                    break;
                default:
                    // all other instructions
                    perfScoreUnhandledInstruction(id, &result);
                    break;
            }
            break;

        case IF_SVE_JF_4A: // ...........mmmmm ...gggnnnnnttttt -- SVE store multiple structures (quadwords, scalar plus
                           // scalar)
            switch (ins)
            {
                case INS_sve_st2q:
                    result.insThroughput = PERFSCORE_THROUGHPUT_1C; // need to fix
                    result.insLatency    = PERFSCORE_LATENCY_1C;    // need to fix
                    break;
                case INS_sve_st3q:
                    result.insThroughput = PERFSCORE_THROUGHPUT_1C; // need to fix
                    result.insLatency    = PERFSCORE_LATENCY_1C;    // need to fix
                    break;
                case INS_sve_st4q:
                    result.insThroughput = PERFSCORE_THROUGHPUT_1C; // need to fix
                    result.insLatency    = PERFSCORE_LATENCY_1C;    // need to fix
                    break;
                default:
                    // all other instructions
                    perfScoreUnhandledInstruction(id, &result);
                    break;
            }
            break;

        case IF_SVE_JJ_4B:   // ...........mmmmm ...gggnnnnnttttt -- SVE 64-bit scatter store (scalar plus 64-bit scaled
                             // offsets)
        case IF_SVE_JJ_4B_C: // ...........mmmmm ...gggnnnnnttttt -- SVE 64-bit scatter store (scalar plus 64-bit scaled
                             // offsets)
        case IF_SVE_JJ_4B_E: // ...........mmmmm ...gggnnnnnttttt -- SVE 64-bit scatter store (scalar plus 64-bit scaled
                             // offsets)
        case IF_SVE_JK_4B: // ...........mmmmm ...gggnnnnnttttt -- SVE 64-bit scatter store (scalar plus 64-bit unscaled
                           // offsets)
            result.insThroughput = PERFSCORE_THROUGHPUT_1C;
            result.insLatency    = PERFSCORE_LATENCY_2C;
            break;

        case IF_SVE_GP_3A: // ........xx.....r ...gggmmmmmddddd -- SVE floating-point complex add (predicated)
        case IF_SVE_EI_3A: // ...........mmmmm ......nnnnnddddd -- SVE mixed sign dot product
            result.insThroughput = PERFSCORE_THROUGHPUT_2C;
            result.insLatency    = PERFSCORE_LATENCY_3C;
            break;

        case IF_SVE_GV_3A: // ...........immmm ....rrnnnnnddddd -- SVE floating-point complex multiply-add (indexed)
        case IF_SVE_GT_4A: // ........xx.mmmmm .rrgggnnnnnddddd -- SVE floating-point complex multiply-add (predicated)
        case IF_SVE_HD_3A: // ...........mmmmm ......nnnnnddddd -- SVE floating point matrix multiply accumulate
            result.insThroughput = PERFSCORE_THROUGHPUT_2C;
            result.insLatency    = PERFSCORE_LATENCY_5C;
            break;

        case IF_SVE_HI_3A: // ........xx...... ...gggnnnnn.DDDD -- SVE floating-point compare with zero
            result.insThroughput = PERFSCORE_THROUGHPUT_1C;
            result.insLatency    = PERFSCORE_LATENCY_2C;
            break;

        case IF_SVE_HM_2A: // ........xx...... ...ggg....iddddd -- SVE floating-point arithmetic with immediate
                           // (predicated)
            switch (ins)
            {
                case INS_sve_fmul:
                    result.insThroughput = PERFSCORE_THROUGHPUT_2C;
                    result.insLatency    = PERFSCORE_LATENCY_3C;
                    break;

                default:
                    result.insThroughput = PERFSCORE_THROUGHPUT_2C;
                    result.insLatency    = PERFSCORE_LATENCY_2C;
                    break;
            }
            break;

        case IF_SVE_HN_2A: // ........xx...iii ......mmmmmddddd -- SVE floating-point trig multiply-add coefficient
            result.insThroughput = PERFSCORE_THROUGHPUT_2C;
            result.insLatency    = PERFSCORE_LATENCY_4C;
            break;

        case IF_SVE_HP_3A: // .............xx. ...gggnnnnnddddd -- SVE floating-point convert to integer
            result.insThroughput = PERFSCORE_THROUGHPUT_1C;
            result.insLatency    = PERFSCORE_LATENCY_3C;
            break;

        case IF_SVE_HU_4B: // ...........mmmmm ...gggnnnnnddddd -- SVE floating-point multiply-accumulate writing addend
            switch (ins)
            {
                case INS_sve_bfmla:
                    result.insThroughput = PERFSCORE_THROUGHPUT_1C; // need to fix
                    result.insLatency    = PERFSCORE_LATENCY_1C;    // need to fix
                    break;

                case INS_sve_bfmls:
                    result.insThroughput = PERFSCORE_THROUGHPUT_1C; // need to fix
                    result.insLatency    = PERFSCORE_LATENCY_1C;    // need to fix
                    break;

                default:
                    // all other instructions
                    perfScoreUnhandledInstruction(id, &result);
                    break;
            }
            break;

        case IF_SVE_HV_4A: // ........xx.aaaaa ...gggmmmmmddddd -- SVE floating-point multiply-accumulate writing
                           // multiplicand
        case IF_SVE_HU_4A: // ........xx.mmmmm ...gggnnnnnddddd -- SVE floating-point multiply-accumulate writing addend
            result.insThroughput = PERFSCORE_THROUGHPUT_2C;
            result.insLatency    = PERFSCORE_LATENCY_4C;
            break;

        case IF_SVE_ID_2A: // ..........iiiiii ...iiinnnnn.TTTT -- SVE load predicate register
        case IF_SVE_IE_2A: // ..........iiiiii ...iiinnnnnttttt -- SVE load vector register
            result.insThroughput = PERFSCORE_THROUGHPUT_3C;
            result.insLatency    = PERFSCORE_LATENCY_6C;
            break;

        case IF_SVE_JG_2A: // ..........iiiiii ...iiinnnnn.TTTT -- SVE store predicate register
        case IF_SVE_JH_2A: // ..........iiiiii ...iiinnnnnttttt -- SVE store vector register
            result.insThroughput = PERFSCORE_THROUGHPUT_2C;
            result.insLatency    = PERFSCORE_LATENCY_2C;
            break;

        case IF_SVE_GG_3A: // ........ii.mmmmm ......nnnnnddddd -- SVE2 lookup table with 2-bit indices and 16-bit
                           // element size
            result.insThroughput = PERFSCORE_THROUGHPUT_1C; // need to fix
            result.insLatency    = PERFSCORE_LATENCY_1C;    // need to fix
            break;

        case IF_SVE_GH_3B: // ........ii.mmmmm ......nnnnnddddd -- SVE2 lookup table with 4-bit indices and 16-bit
                           // element size
            result.insThroughput = PERFSCORE_THROUGHPUT_1C; // need to fix
            result.insLatency    = PERFSCORE_LATENCY_1C;    // need to fix
            break;

        case IF_SVE_GH_3B_B: // ........ii.mmmmm ......nnnnnddddd -- SVE2 lookup table with 4-bit indices and 16-bit
                             // element size
            result.insThroughput = PERFSCORE_THROUGHPUT_1C; // need to fix
            result.insLatency    = PERFSCORE_LATENCY_1C;    // need to fix
            break;

        case IF_SVE_GG_3B: // ........ii.mmmmm ...i..nnnnnddddd -- SVE2 lookup table with 2-bit indices and 16-bit
                           // element size
            result.insThroughput = PERFSCORE_THROUGHPUT_1C; // need to fix
            result.insLatency    = PERFSCORE_LATENCY_1C;    // need to fix
            break;

        case IF_SVE_GH_3A: // ........i..mmmmm ......nnnnnddddd -- SVE2 lookup table with 4-bit indices and 16-bit
                           // element size
            result.insThroughput = PERFSCORE_THROUGHPUT_1C; // need to fix
            result.insLatency    = PERFSCORE_LATENCY_1C;    // need to fix
            break;

        case IF_SVE_HY_3A: // .........h.mmmmm ...gggnnnnn.oooo -- SVE 32-bit gather prefetch (scalar plus 32-bit scaled
                           // offsets)
            switch (ins)
            {
                case INS_sve_prfb:
                    result.insThroughput = PERFSCORE_THROUGHPUT_1C; // need to fix
                    result.insLatency    = PERFSCORE_LATENCY_1C;    // need to fix
                    break;
                case INS_sve_prfh:
                    result.insThroughput = PERFSCORE_THROUGHPUT_1C; // need to fix
                    result.insLatency    = PERFSCORE_LATENCY_1C;    // need to fix
                    break;
                case INS_sve_prfw:
                    result.insThroughput = PERFSCORE_THROUGHPUT_1C; // need to fix
                    result.insLatency    = PERFSCORE_LATENCY_1C;    // need to fix
                    break;
                case INS_sve_prfd:
                    result.insThroughput = PERFSCORE_THROUGHPUT_1C; // need to fix
                    result.insLatency    = PERFSCORE_LATENCY_1C;    // need to fix
                    break;
                default:
                    // all other instructions
                    perfScoreUnhandledInstruction(id, &result);
                    break;
            }
            break;

        case IF_SVE_HY_3A_A: // .........h.mmmmm ...gggnnnnn.oooo -- SVE 32-bit gather prefetch (scalar plus 32-bit
                             // scaled offsets)
            switch (ins)
            {
                case INS_sve_prfb:
                    result.insThroughput = PERFSCORE_THROUGHPUT_1C; // need to fix
                    result.insLatency    = PERFSCORE_LATENCY_1C;    // need to fix
                    break;
                case INS_sve_prfh:
                    result.insThroughput = PERFSCORE_THROUGHPUT_1C; // need to fix
                    result.insLatency    = PERFSCORE_LATENCY_1C;    // need to fix
                    break;
                case INS_sve_prfw:
                    result.insThroughput = PERFSCORE_THROUGHPUT_1C; // need to fix
                    result.insLatency    = PERFSCORE_LATENCY_1C;    // need to fix
                    break;
                case INS_sve_prfd:
                    result.insThroughput = PERFSCORE_THROUGHPUT_1C; // need to fix
                    result.insLatency    = PERFSCORE_LATENCY_1C;    // need to fix
                    break;
                default:
                    // all other instructions
                    perfScoreUnhandledInstruction(id, &result);
                    break;
            }
            break;

        case IF_SVE_HY_3B: // ...........mmmmm ...gggnnnnn.oooo -- SVE 32-bit gather prefetch (scalar plus 32-bit scaled
                           // offsets)
            switch (ins)
            {
                case INS_sve_prfb:
                    result.insThroughput = PERFSCORE_THROUGHPUT_1C; // need to fix
                    result.insLatency    = PERFSCORE_LATENCY_1C;    // need to fix
                    break;
                case INS_sve_prfh:
                    result.insThroughput = PERFSCORE_THROUGHPUT_1C; // need to fix
                    result.insLatency    = PERFSCORE_LATENCY_1C;    // need to fix
                    break;
                case INS_sve_prfw:
                    result.insThroughput = PERFSCORE_THROUGHPUT_1C; // need to fix
                    result.insLatency    = PERFSCORE_LATENCY_1C;    // need to fix
                    break;
                case INS_sve_prfd:
                    result.insThroughput = PERFSCORE_THROUGHPUT_1C; // need to fix
                    result.insLatency    = PERFSCORE_LATENCY_1C;    // need to fix
                    break;
                default:
                    // all other instructions
                    perfScoreUnhandledInstruction(id, &result);
                    break;
            }
            break;

        case IF_SVE_IB_3A: // ...........mmmmm ...gggnnnnn.oooo -- SVE contiguous prefetch (scalar plus scalar)
            switch (ins)
            {
                case INS_sve_prfb:
                    result.insThroughput = PERFSCORE_THROUGHPUT_1C; // need to fix
                    result.insLatency    = PERFSCORE_LATENCY_1C;    // need to fix
                    break;
                case INS_sve_prfh:
                    result.insThroughput = PERFSCORE_THROUGHPUT_1C; // need to fix
                    result.insLatency    = PERFSCORE_LATENCY_1C;    // need to fix
                    break;
                case INS_sve_prfw:
                    result.insThroughput = PERFSCORE_THROUGHPUT_1C; // need to fix
                    result.insLatency    = PERFSCORE_LATENCY_1C;    // need to fix
                    break;
                case INS_sve_prfd:
                    result.insThroughput = PERFSCORE_THROUGHPUT_1C; // need to fix
                    result.insLatency    = PERFSCORE_LATENCY_1C;    // need to fix
                    break;
                default:
                    // all other instructions
                    perfScoreUnhandledInstruction(id, &result);
                    break;
            }
            break;

        case IF_SVE_HZ_2A_B: // ...........iiiii ...gggnnnnn.oooo -- SVE 32-bit gather prefetch (vector plus immediate)
            switch (ins)
            {
                case INS_sve_prfb:
                    result.insThroughput = PERFSCORE_THROUGHPUT_1C; // need to fix
                    result.insLatency    = PERFSCORE_LATENCY_1C;    // need to fix
                    break;
                case INS_sve_prfh:
                    result.insThroughput = PERFSCORE_THROUGHPUT_1C; // need to fix
                    result.insLatency    = PERFSCORE_LATENCY_1C;    // need to fix
                    break;
                case INS_sve_prfw:
                    result.insThroughput = PERFSCORE_THROUGHPUT_1C; // need to fix
                    result.insLatency    = PERFSCORE_LATENCY_1C;    // need to fix
                    break;
                case INS_sve_prfd:
                    result.insThroughput = PERFSCORE_THROUGHPUT_1C; // need to fix
                    result.insLatency    = PERFSCORE_LATENCY_1C;    // need to fix
                    break;
                default:
                    // all other instructions
                    perfScoreUnhandledInstruction(id, &result);
                    break;
            }
            break;

        case IF_SVE_IA_2A: // ..........iiiiii ...gggnnnnn.oooo -- SVE contiguous prefetch (scalar plus immediate)
            switch (ins)
            {
                case INS_sve_prfb:
                    result.insThroughput = PERFSCORE_THROUGHPUT_1C; // need to fix
                    result.insLatency    = PERFSCORE_LATENCY_1C;    // need to fix
                    break;
                case INS_sve_prfh:
                    result.insThroughput = PERFSCORE_THROUGHPUT_1C; // need to fix
                    result.insLatency    = PERFSCORE_LATENCY_1C;    // need to fix
                    break;
                case INS_sve_prfw:
                    result.insThroughput = PERFSCORE_THROUGHPUT_1C; // need to fix
                    result.insLatency    = PERFSCORE_LATENCY_1C;    // need to fix
                    break;
                case INS_sve_prfd:
                    result.insThroughput = PERFSCORE_THROUGHPUT_1C; // need to fix
                    result.insLatency    = PERFSCORE_LATENCY_1C;    // need to fix
                    break;
                default:
                    // all other instructions
                    perfScoreUnhandledInstruction(id, &result);
                    break;
            }
            break;

        case IF_SVE_HX_3A_B: // ...........iiiii ...gggnnnnnttttt -- SVE 32-bit gather load (vector plus immediate)
        case IF_SVE_HX_3A_E: // ...........iiiii ...gggnnnnnttttt -- SVE 32-bit gather load (vector plus immediate)
        case IF_SVE_IV_3A:   // ...........iiiii ...gggnnnnnttttt -- SVE 64-bit gather load (vector plus immediate)
            result.insThroughput = PERFSCORE_THROUGHPUT_1C;
            result.insLatency    = PERFSCORE_LATENCY_9C;
            break;

        case IF_SVE_JI_3A_A: // ...........iiiii ...gggnnnnnttttt -- SVE 32-bit scatter store (vector plus immediate)
        case IF_SVE_JL_3A:   // ...........iiiii ...gggnnnnnttttt -- SVE 64-bit scatter store (vector plus immediate)
            result.insThroughput = PERFSCORE_THROUGHPUT_1C;
            result.insLatency    = PERFSCORE_LATENCY_2C;
            break;

        case IF_SVE_IC_3A:   // ..........iiiiii ...gggnnnnnttttt -- SVE load and broadcast element
        case IF_SVE_IC_3A_A: // ..........iiiiii ...gggnnnnnttttt -- SVE load and broadcast element
        case IF_SVE_IC_3A_B: // ..........iiiiii ...gggnnnnnttttt -- SVE load and broadcast element
        case IF_SVE_IC_3A_C: // ..........iiiiii ...gggnnnnnttttt -- SVE load and broadcast element
            result.insThroughput = PERFSCORE_THROUGHPUT_3C;
            result.insLatency    = PERFSCORE_LATENCY_6C;
            break;

        case IF_SVE_BI_2A: // ................ ......nnnnnddddd -- SVE constructive prefix (unpredicated)
            result.insThroughput = PERFSCORE_THROUGHPUT_2C;
            result.insLatency    = PERFSCORE_LATENCY_2C;
            break;

        case IF_SVE_HH_2A: // ................ ......nnnnnddddd -- SVE2 FP8 upconverts
            switch (ins)
            {
                case INS_sve_f1cvt:
                    result.insThroughput = PERFSCORE_THROUGHPUT_1C; // need to fix
                    result.insLatency    = PERFSCORE_LATENCY_1C;    // need to fix
                    break;
                case INS_sve_f2cvt:
                    result.insThroughput = PERFSCORE_THROUGHPUT_1C; // need to fix
                    result.insLatency    = PERFSCORE_LATENCY_1C;    // need to fix
                    break;
                case INS_sve_bf1cvt:
                    result.insThroughput = PERFSCORE_THROUGHPUT_1C; // need to fix
                    result.insLatency    = PERFSCORE_LATENCY_1C;    // need to fix
                    break;
                case INS_sve_bf2cvt:
                    result.insThroughput = PERFSCORE_THROUGHPUT_1C; // need to fix
                    result.insLatency    = PERFSCORE_LATENCY_1C;    // need to fix
                    break;
                case INS_sve_f1cvtlt:
                    result.insThroughput = PERFSCORE_THROUGHPUT_1C; // need to fix
                    result.insLatency    = PERFSCORE_LATENCY_1C;    // need to fix
                    break;
                case INS_sve_f2cvtlt:
                    result.insThroughput = PERFSCORE_THROUGHPUT_1C; // need to fix
                    result.insLatency    = PERFSCORE_LATENCY_1C;    // need to fix
                    break;
                case INS_sve_bf1cvtlt:
                    result.insThroughput = PERFSCORE_THROUGHPUT_1C; // need to fix
                    result.insLatency    = PERFSCORE_LATENCY_1C;    // need to fix
                    break;
                case INS_sve_bf2cvtlt:
                    result.insThroughput = PERFSCORE_THROUGHPUT_1C; // need to fix
                    result.insLatency    = PERFSCORE_LATENCY_1C;    // need to fix
                    break;
                default:
                    // all other instructions
                    perfScoreUnhandledInstruction(id, &result);
                    break;
            }
            break;

        case IF_SVE_BJ_2A: // ........xx...... ......nnnnnddddd -- SVE floating-point exponential accelerator
            result.insThroughput = PERFSCORE_THROUGHPUT_1C;
            result.insLatency    = PERFSCORE_LATENCY_3C;
            break;

        case IF_SVE_CB_2A: // ........xx...... ......nnnnnddddd -- SVE broadcast general register
            switch (ins)
            {
                case INS_sve_mov:
                    result.insThroughput = PERFSCORE_THROUGHPUT_2C;
                    result.insLatency    = PERFSCORE_LATENCY_2C;
                    break;
                case INS_sve_dup:
                    result.insThroughput = PERFSCORE_THROUGHPUT_1C;
                    result.insLatency    = PERFSCORE_LATENCY_3C;
                    break;
                default:
                    // all other instructions
                    perfScoreUnhandledInstruction(id, &result);
                    break;
            }
            break;

        case IF_SVE_CG_2A: // ........xx...... ......nnnnnddddd -- SVE reverse vector elements
            switch (ins)
            {
                case INS_sve_rev:
                    result.insThroughput = PERFSCORE_THROUGHPUT_2C;
                    result.insLatency    = PERFSCORE_LATENCY_2C;
                    break;
                default:
                    // all other instructions
                    perfScoreUnhandledInstruction(id, &result);
                    break;
            }
            break;

        case IF_SVE_CH_2A: // ........xx...... ......nnnnnddddd -- SVE unpack vector elements
            result.insThroughput = PERFSCORE_THROUGHPUT_2C;
            result.insLatency    = PERFSCORE_LATENCY_2C;
            break;

        case IF_SVE_HF_2A: // ........xx...... ......nnnnnddddd -- SVE floating-point reciprocal estimate (unpredicated)
            result.insThroughput = PERFSCORE_THROUGHPUT_1C;
            result.insLatency    = PERFSCORE_LATENCY_3C;
            break;

        case IF_SVE_BF_2A: // ........xx.xxiii ......nnnnnddddd -- SVE bitwise shift by immediate (unpredicated)
        case IF_SVE_FT_2A: // ........xx.xxiii ......nnnnnddddd -- SVE2 bitwise shift and insert
            result.insThroughput = PERFSCORE_THROUGHPUT_1C;
            result.insLatency    = PERFSCORE_LATENCY_2C;
            break;

        case IF_SVE_FU_2A: // ........xx.xxiii ......nnnnnddddd -- SVE2 bitwise shift right and accumulate
            result.insThroughput = PERFSCORE_THROUGHPUT_1C;
            result.insLatency    = PERFSCORE_LATENCY_4C;
            break;

        case IF_SVE_BX_2A:                                  // ...........ixxxx ......nnnnnddddd -- sve_int_perm_dupq_i
            result.insThroughput = PERFSCORE_THROUGHPUT_1C; // need to fix
            result.insLatency    = PERFSCORE_LATENCY_1C;    // need to fix
            break;

        case IF_SVE_BY_2A:                                  // ............iiii ......mmmmmddddd -- sve_int_perm_extq
            result.insThroughput = PERFSCORE_THROUGHPUT_1C; // need to fix
            result.insLatency    = PERFSCORE_LATENCY_1C;    // need to fix
            break;

        default:
            // all other instructions
            perfScoreUnhandledInstruction(id, &result);
            break;
    }
}
#endif // defined(DEBUG) || defined(LATE_DISASM)

#endif // TARGET_ARM64
