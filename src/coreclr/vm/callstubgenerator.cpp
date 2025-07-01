// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifdef FEATURE_INTERPRETER

#include "callstubgenerator.h"
#include "ecall.h"

extern "C" void Load_Stack();
extern "C" void Store_Stack();

#if defined(TARGET_APPLE) && defined(TARGET_ARM64)
extern "C" void Load_Stack_1B();
extern "C" void Load_Stack_2B();
extern "C" void Load_Stack_4B();

extern "C" void Store_Stack_1B();
extern "C" void Store_Stack_2B();
extern "C" void Store_Stack_4B();
#endif // TARGET_APPLE && TARGET_ARM64

#ifdef TARGET_AMD64

#ifdef TARGET_WINDOWS
extern "C" void Load_RCX();
extern "C" void Load_RCX_RDX();
extern "C" void Load_RCX_RDX_R8();
extern "C" void Load_RCX_RDX_R8_R9();
extern "C" void Load_RDX();
extern "C" void Load_RDX_R8();
extern "C" void Load_RDX_R8_R9();
extern "C" void Load_R8();
extern "C" void Load_R8_R9();
extern "C" void Load_R9();
extern "C" void Load_XMM0();
extern "C" void Load_XMM0_XMM1();
extern "C" void Load_XMM0_XMM1_XMM2();
extern "C" void Load_XMM0_XMM1_XMM2_XMM3();
extern "C" void Load_XMM1();
extern "C" void Load_XMM1_XMM2();
extern "C" void Load_XMM1_XMM2_XMM3();
extern "C" void Load_XMM2();
extern "C" void Load_XMM2_XMM3();
extern "C" void Load_XMM3();
extern "C" void Load_Ref_RCX();
extern "C" void Load_Ref_RDX();
extern "C" void Load_Ref_R8();
extern "C" void Load_Ref_R9();

extern "C" void Store_RCX();
extern "C" void Store_RCX_RDX();
extern "C" void Store_RCX_RDX_R8();
extern "C" void Store_RCX_RDX_R8_R9();
extern "C" void Store_RDX();
extern "C" void Store_RDX_R8();
extern "C" void Store_RDX_R8_R9();
extern "C" void Store_R8();
extern "C" void Store_R8_R9();
extern "C" void Store_R9();
extern "C" void Store_XMM0();
extern "C" void Store_XMM0_XMM1();
extern "C" void Store_XMM0_XMM1_XMM2();
extern "C" void Store_XMM0_XMM1_XMM2_XMM3();
extern "C" void Store_XMM1();
extern "C" void Store_XMM1_XMM2();
extern "C" void Store_XMM1_XMM2_XMM3();
extern "C" void Store_XMM2();
extern "C" void Store_XMM2_XMM3();
extern "C" void Store_XMM3();
extern "C" void Store_Ref_RCX();
extern "C" void Store_Ref_RDX();
extern "C" void Store_Ref_R8();
extern "C" void Store_Ref_R9();

PCODE GPRegsRoutines[] =
{
    (PCODE)Load_RCX,            // 00
    (PCODE)Load_RCX_RDX,        // 01
    (PCODE)Load_RCX_RDX_R8,     // 02
    (PCODE)Load_RCX_RDX_R8_R9,  // 03
    (PCODE)0,                   // 10
    (PCODE)Load_RDX,            // 11
    (PCODE)Load_RDX_R8,         // 12
    (PCODE)Load_RDX_R8_R9,      // 13
    (PCODE)0,                   // 20
    (PCODE)0,                   // 21
    (PCODE)Load_R8,             // 22
    (PCODE)Load_R8_R9,          // 23
    (PCODE)0,                   // 30
    (PCODE)0,                   // 31
    (PCODE)0,                   // 32
    (PCODE)Load_R9              // 33
};

PCODE GPRegsStoreRoutines[] =
{
    (PCODE)Store_RCX,            // 00
    (PCODE)Store_RCX_RDX,        // 01
    (PCODE)Store_RCX_RDX_R8,     // 02
    (PCODE)Store_RCX_RDX_R8_R9,  // 03
    (PCODE)0,                    // 10
    (PCODE)Store_RDX,            // 11
    (PCODE)Store_RDX_R8,         // 12
    (PCODE)Store_RDX_R8_R9,      // 13
    (PCODE)0,                    // 20
    (PCODE)0,                    // 21
    (PCODE)Store_R8,             // 22
    (PCODE)Store_R8_R9,          // 23
    (PCODE)0,                    // 30
    (PCODE)0,                    // 31
    (PCODE)0,                    // 32
    (PCODE)Store_R9              // 33
};

PCODE GPRegsRefRoutines[] =
{
    (PCODE)Load_Ref_RCX,        // 0
    (PCODE)Load_Ref_RDX,        // 1
    (PCODE)Load_Ref_R8,         // 2
    (PCODE)Load_Ref_R9,         // 3
};

PCODE GPRegsRefStoreRoutines[] =
{
    (PCODE)Store_Ref_RCX,        // 0
    (PCODE)Store_Ref_RDX,        // 1
    (PCODE)Store_Ref_R8,         // 2
    (PCODE)Store_Ref_R9,         // 3
};

PCODE FPRegsRoutines[] =
{
    (PCODE)Load_XMM0,                // 00
    (PCODE)Load_XMM0_XMM1,           // 01
    (PCODE)Load_XMM0_XMM1_XMM2,      // 02
    (PCODE)Load_XMM0_XMM1_XMM2_XMM3, // 03
    (PCODE)0,                        // 10
    (PCODE)Load_XMM1,                // 11
    (PCODE)Load_XMM1_XMM2,           // 12
    (PCODE)Load_XMM1_XMM2_XMM3,      // 13
    (PCODE)0,                        // 20
    (PCODE)0,                        // 21
    (PCODE)Load_XMM2,                // 22
    (PCODE)Load_XMM2_XMM3,           // 23
    (PCODE)0,                        // 30
    (PCODE)0,                        // 31
    (PCODE)0,                        // 32
    (PCODE)Load_XMM3                 // 33
};

PCODE FPRegsStoreRoutines[] =
{
    (PCODE)Store_XMM0,                // 00
    (PCODE)Store_XMM0_XMM1,           // 01
    (PCODE)Store_XMM0_XMM1_XMM2,      // 02
    (PCODE)Store_XMM0_XMM1_XMM2_XMM3, // 03
    (PCODE)0,                         // 10
    (PCODE)Store_XMM1,                // 11
    (PCODE)Store_XMM1_XMM2,           // 12
    (PCODE)Store_XMM1_XMM2_XMM3,      // 13
    (PCODE)0,                         // 20
    (PCODE)0,                         // 21
    (PCODE)Store_XMM2,                // 22
    (PCODE)Store_XMM2_XMM3,           // 23
    (PCODE)0,                         // 30
    (PCODE)0,                         // 31
    (PCODE)0,                         // 32
    (PCODE)Store_XMM3                 // 33
};

#else // TARGET_WINDOWS

extern "C" void Load_RDI();
extern "C" void Load_RDI_RSI();
extern "C" void Load_RDI_RSI_RDX();
extern "C" void Load_RDI_RSI_RDX_RCX();
extern "C" void Load_RDI_RSI_RDX_RCX_R8();
extern "C" void Load_RDI_RSI_RDX_RCX_R8_R9();
extern "C" void Load_RSI();
extern "C" void Load_RSI_RDX();
extern "C" void Load_RSI_RDX_RCX();
extern "C" void Load_RSI_RDX_RCX_R8();
extern "C" void Load_RSI_RDX_RCX_R8_R9();
extern "C" void Load_RDX();
extern "C" void Load_RDX_RCX();
extern "C" void Load_RDX_RCX_R8();
extern "C" void Load_RDX_RCX_R8_R9();
extern "C" void Load_RCX();
extern "C" void Load_RCX_R8();
extern "C" void Load_RCX_R8_R9();
extern "C" void Load_R8();
extern "C" void Load_R8_R9();
extern "C" void Load_R9();

extern "C" void Store_RDI();
extern "C" void Store_RDI_RSI();
extern "C" void Store_RDI_RSI_RDX();
extern "C" void Store_RDI_RSI_RDX_RCX();
extern "C" void Store_RDI_RSI_RDX_RCX_R8();
extern "C" void Store_RDI_RSI_RDX_RCX_R8_R9();
extern "C" void Store_RSI();
extern "C" void Store_RSI_RDX();
extern "C" void Store_RSI_RDX_RCX();
extern "C" void Store_RSI_RDX_RCX_R8();
extern "C" void Store_RSI_RDX_RCX_R8_R9();
extern "C" void Store_RDX();
extern "C" void Store_RDX_RCX();
extern "C" void Store_RDX_RCX_R8();
extern "C" void Store_RDX_RCX_R8_R9();
extern "C" void Store_RCX();
extern "C" void Store_RCX_R8();
extern "C" void Store_RCX_R8_R9();
extern "C" void Store_R8();
extern "C" void Store_R8_R9();
extern "C" void Store_R9();

PCODE GPRegsRoutines[] =
{
    (PCODE)Load_RDI,                    // 00
    (PCODE)Load_RDI_RSI,                // 01
    (PCODE)Load_RDI_RSI_RDX,            // 02
    (PCODE)Load_RDI_RSI_RDX_RCX,        // 03
    (PCODE)Load_RDI_RSI_RDX_RCX_R8,     // 04
    (PCODE)Load_RDI_RSI_RDX_RCX_R8_R9,  // 05
    (PCODE)0,                           // 10
    (PCODE)Load_RSI,                    // 11
    (PCODE)Load_RSI_RDX,                // 12
    (PCODE)Load_RSI_RDX_RCX,            // 13
    (PCODE)Load_RSI_RDX_RCX_R8,         // 14
    (PCODE)Load_RSI_RDX_RCX_R8_R9,      // 15
    (PCODE)0,                           // 20
    (PCODE)0,                           // 21
    (PCODE)Load_RDX,                    // 22
    (PCODE)Load_RDX_RCX,                // 23
    (PCODE)Load_RDX_RCX_R8,             // 24
    (PCODE)Load_RDX_RCX_R8_R9,          // 25
    (PCODE)0,                           // 30
    (PCODE)0,                           // 31
    (PCODE)0,                           // 32
    (PCODE)Load_RCX,                    // 33
    (PCODE)Load_RCX_R8,                 // 34
    (PCODE)Load_RCX_R8_R9,              // 35
    (PCODE)0,                           // 40
    (PCODE)0,                           // 41
    (PCODE)0,                           // 42
    (PCODE)0,                           // 43
    (PCODE)Load_R8,                     // 44
    (PCODE)Load_R8_R9,                  // 45
    (PCODE)0,                           // 50
    (PCODE)0,                           // 51
    (PCODE)0,                           // 52
    (PCODE)0,                           // 53
    (PCODE)0,                           // 54
    (PCODE)Load_R9                      // 55
};

PCODE GPRegsStoreRoutines[] =
{
    (PCODE)Store_RDI,                    // 00
    (PCODE)Store_RDI_RSI,                // 01
    (PCODE)Store_RDI_RSI_RDX,            // 02
    (PCODE)Store_RDI_RSI_RDX_RCX,        // 03
    (PCODE)Store_RDI_RSI_RDX_RCX_R8,     // 04
    (PCODE)Store_RDI_RSI_RDX_RCX_R8_R9,  // 05
    (PCODE)0,                            // 10
    (PCODE)Store_RSI,                    // 11
    (PCODE)Store_RSI_RDX,                // 12
    (PCODE)Store_RSI_RDX_RCX,            // 13
    (PCODE)Store_RSI_RDX_RCX_R8,         // 14
    (PCODE)Store_RSI_RDX_RCX_R8_R9,      // 15
    (PCODE)0,                            // 20
    (PCODE)0,                            // 21
    (PCODE)Store_RDX,                    // 22
    (PCODE)Store_RDX_RCX,                // 23
    (PCODE)Store_RDX_RCX_R8,             // 24
    (PCODE)Store_RDX_RCX_R8_R9,          // 25
    (PCODE)0,                            // 30
    (PCODE)0,                            // 31
    (PCODE)0,                            // 32
    (PCODE)Store_RCX,                    // 33
    (PCODE)Store_RCX_R8,                 // 34
    (PCODE)Store_RCX_R8_R9,              // 35
    (PCODE)0,                            // 40
    (PCODE)0,                            // 41
    (PCODE)0,                            // 42
    (PCODE)0,                            // 43
    (PCODE)Store_R8,                     // 44
    (PCODE)Store_R8_R9,                  // 45
    (PCODE)0,                            // 50
    (PCODE)0,                            // 51
    (PCODE)0,                            // 52
    (PCODE)0,                            // 53
    (PCODE)0,                            // 54
    (PCODE)Store_R9                      // 55
};

extern "C" void Load_XMM0();
extern "C" void Load_XMM0_XMM1();
extern "C" void Load_XMM0_XMM1_XMM2();
extern "C" void Load_XMM0_XMM1_XMM2_XMM3();
extern "C" void Load_XMM0_XMM1_XMM2_XMM3_XMM4();
extern "C" void Load_XMM0_XMM1_XMM2_XMM3_XMM4_XMM5();
extern "C" void Load_XMM0_XMM1_XMM2_XMM3_XMM4_XMM5_XMM6();
extern "C" void Load_XMM0_XMM1_XMM2_XMM3_XMM4_XMM5_XMM6_XMM7();
extern "C" void Load_XMM1();
extern "C" void Load_XMM1_XMM2();
extern "C" void Load_XMM1_XMM2_XMM3();
extern "C" void Load_XMM1_XMM2_XMM3_XMM4();
extern "C" void Load_XMM1_XMM2_XMM3_XMM4_XMM5();
extern "C" void Load_XMM1_XMM2_XMM3_XMM4_XMM5_XMM6();
extern "C" void Load_XMM1_XMM2_XMM3_XMM4_XMM5_XMM6_XMM7();
extern "C" void Load_XMM2();
extern "C" void Load_XMM2_XMM3();
extern "C" void Load_XMM2_XMM3_XMM4();
extern "C" void Load_XMM2_XMM3_XMM4_XMM5();
extern "C" void Load_XMM2_XMM3_XMM4_XMM5_XMM6();
extern "C" void Load_XMM2_XMM3_XMM4_XMM5_XMM6_XMM7();
extern "C" void Load_XMM3();
extern "C" void Load_XMM3_XMM4();
extern "C" void Load_XMM3_XMM4_XMM5();
extern "C" void Load_XMM3_XMM4_XMM5_XMM6();
extern "C" void Load_XMM3_XMM4_XMM5_XMM6_XMM7();
extern "C" void Load_XMM4();
extern "C" void Load_XMM4_XMM5();
extern "C" void Load_XMM4_XMM5_XMM6();
extern "C" void Load_XMM4_XMM5_XMM6_XMM7();
extern "C" void Load_XMM5();
extern "C" void Load_XMM5_XMM6();
extern "C" void Load_XMM5_XMM6_XMM7();
extern "C" void Load_XMM6();
extern "C" void Load_XMM6_XMM7();
extern "C" void Load_XMM7();

extern "C" void Store_XMM0();
extern "C" void Store_XMM0_XMM1();
extern "C" void Store_XMM0_XMM1_XMM2();
extern "C" void Store_XMM0_XMM1_XMM2_XMM3();
extern "C" void Store_XMM0_XMM1_XMM2_XMM3_XMM4();
extern "C" void Store_XMM0_XMM1_XMM2_XMM3_XMM4_XMM5();
extern "C" void Store_XMM0_XMM1_XMM2_XMM3_XMM4_XMM5_XMM6();
extern "C" void Store_XMM0_XMM1_XMM2_XMM3_XMM4_XMM5_XMM6_XMM7();
extern "C" void Store_XMM1();
extern "C" void Store_XMM1_XMM2();
extern "C" void Store_XMM1_XMM2_XMM3();
extern "C" void Store_XMM1_XMM2_XMM3_XMM4();
extern "C" void Store_XMM1_XMM2_XMM3_XMM4_XMM5();
extern "C" void Store_XMM1_XMM2_XMM3_XMM4_XMM5_XMM6();
extern "C" void Store_XMM1_XMM2_XMM3_XMM4_XMM5_XMM6_XMM7();
extern "C" void Store_XMM2();
extern "C" void Store_XMM2_XMM3();
extern "C" void Store_XMM2_XMM3_XMM4();
extern "C" void Store_XMM2_XMM3_XMM4_XMM5();
extern "C" void Store_XMM2_XMM3_XMM4_XMM5_XMM6();
extern "C" void Store_XMM2_XMM3_XMM4_XMM5_XMM6_XMM7();
extern "C" void Store_XMM3();
extern "C" void Store_XMM3_XMM4();
extern "C" void Store_XMM3_XMM4_XMM5();
extern "C" void Store_XMM3_XMM4_XMM5_XMM6();
extern "C" void Store_XMM3_XMM4_XMM5_XMM6_XMM7();
extern "C" void Store_XMM4();
extern "C" void Store_XMM4_XMM5();
extern "C" void Store_XMM4_XMM5_XMM6();
extern "C" void Store_XMM4_XMM5_XMM6_XMM7();
extern "C" void Store_XMM5();
extern "C" void Store_XMM5_XMM6();
extern "C" void Store_XMM5_XMM6_XMM7();
extern "C" void Store_XMM6();
extern "C" void Store_XMM6_XMM7();
extern "C" void Store_XMM7();

PCODE FPRegsRoutines[] =
{
    (PCODE)Load_XMM0,                                   // 00
    (PCODE)Load_XMM0_XMM1,                              // 01
    (PCODE)Load_XMM0_XMM1_XMM2,                         // 02
    (PCODE)Load_XMM0_XMM1_XMM2_XMM3,                    // 03
    (PCODE)Load_XMM0_XMM1_XMM2_XMM3_XMM4,               // 04
    (PCODE)Load_XMM0_XMM1_XMM2_XMM3_XMM4_XMM5,          // 05
    (PCODE)Load_XMM0_XMM1_XMM2_XMM3_XMM4_XMM5_XMM6,     // 06
    (PCODE)Load_XMM0_XMM1_XMM2_XMM3_XMM4_XMM5_XMM6_XMM7,// 07
    (PCODE)0,                                           // 10
    (PCODE)Load_XMM1,                                   // 11
    (PCODE)Load_XMM1_XMM2,                              // 12
    (PCODE)Load_XMM1_XMM2_XMM3,                         // 13
    (PCODE)Load_XMM1_XMM2_XMM3_XMM4,                    // 14
    (PCODE)Load_XMM1_XMM2_XMM3_XMM4_XMM5,               // 15
    (PCODE)Load_XMM1_XMM2_XMM3_XMM4_XMM5_XMM6,          // 16
    (PCODE)Load_XMM1_XMM2_XMM3_XMM4_XMM5_XMM6_XMM7,     // 17
    (PCODE)0,                                           // 20
    (PCODE)0,                                           // 21
    (PCODE)Load_XMM2,                                   // 22
    (PCODE)Load_XMM2_XMM3,                              // 23
    (PCODE)Load_XMM2_XMM3_XMM4,                         // 24
    (PCODE)Load_XMM2_XMM3_XMM4_XMM5,                    // 25
    (PCODE)Load_XMM2_XMM3_XMM4_XMM5_XMM6,               // 26
    (PCODE)Load_XMM2_XMM3_XMM4_XMM5_XMM6_XMM7,          // 27
    (PCODE)0,                                           // 30
    (PCODE)0,                                           // 31
    (PCODE)0,                                           // 32
    (PCODE)Load_XMM3,                                   // 33
    (PCODE)Load_XMM3_XMM4,                              // 34
    (PCODE)Load_XMM3_XMM4_XMM5,                         // 35
    (PCODE)Load_XMM3_XMM4_XMM5_XMM6,                    // 36
    (PCODE)Load_XMM3_XMM4_XMM5_XMM6_XMM7,               // 37
    (PCODE)0,                                           // 40
    (PCODE)0,                                           // 41
    (PCODE)0,                                           // 42
    (PCODE)0,                                           // 43
    (PCODE)Load_XMM4,                                   // 44
    (PCODE)Load_XMM4_XMM5,                              // 45
    (PCODE)Load_XMM4_XMM5_XMM6,                         // 46
    (PCODE)Load_XMM4_XMM5_XMM6_XMM7,                    // 47
    (PCODE)0,                                           // 50
    (PCODE)0,                                           // 51
    (PCODE)0,                                           // 52
    (PCODE)0,                                           // 53
    (PCODE)0,                                           // 54
    (PCODE)Load_XMM5,                                   // 55
    (PCODE)Load_XMM5_XMM6,                              // 56
    (PCODE)Load_XMM5_XMM6_XMM7,                         // 57
    (PCODE)0,                                           // 60
    (PCODE)0,                                           // 61
    (PCODE)0,                                           // 62
    (PCODE)0,                                           // 63
    (PCODE)0,                                           // 64
    (PCODE)0,                                           // 65
    (PCODE)Load_XMM6,                                   // 66
    (PCODE)Load_XMM6_XMM7,                              // 67
    (PCODE)0,                                           // 70
    (PCODE)0,                                           // 71
    (PCODE)0,                                           // 72
    (PCODE)0,                                           // 73
    (PCODE)0,                                           // 74
    (PCODE)0,                                           // 75
    (PCODE)0,                                           // 76
    (PCODE)Load_XMM7                                    // 77
};

PCODE FPRegsStoreRoutines[] =
{
    (PCODE)Store_XMM0,                                   // 00
    (PCODE)Store_XMM0_XMM1,                              // 01
    (PCODE)Store_XMM0_XMM1_XMM2,                         // 02
    (PCODE)Store_XMM0_XMM1_XMM2_XMM3,                    // 03
    (PCODE)Store_XMM0_XMM1_XMM2_XMM3_XMM4,               // 04
    (PCODE)Store_XMM0_XMM1_XMM2_XMM3_XMM4_XMM5,          // 05
    (PCODE)Store_XMM0_XMM1_XMM2_XMM3_XMM4_XMM5_XMM6,     // 06
    (PCODE)Store_XMM0_XMM1_XMM2_XMM3_XMM4_XMM5_XMM6_XMM7,// 07
    (PCODE)0,                                            // 10
    (PCODE)Store_XMM1,                                   // 11
    (PCODE)Store_XMM1_XMM2,                              // 12
    (PCODE)Store_XMM1_XMM2_XMM3,                         // 13
    (PCODE)Store_XMM1_XMM2_XMM3_XMM4,                    // 14
    (PCODE)Store_XMM1_XMM2_XMM3_XMM4_XMM5,               // 15
    (PCODE)Store_XMM1_XMM2_XMM3_XMM4_XMM5_XMM6,          // 16
    (PCODE)Store_XMM1_XMM2_XMM3_XMM4_XMM5_XMM6_XMM7,     // 17
    (PCODE)0,                                            // 20
    (PCODE)0,                                            // 21
    (PCODE)Store_XMM2,                                   // 22
    (PCODE)Store_XMM2_XMM3,                              // 23
    (PCODE)Store_XMM2_XMM3_XMM4,                         // 24
    (PCODE)Store_XMM2_XMM3_XMM4_XMM5,                    // 25
    (PCODE)Store_XMM2_XMM3_XMM4_XMM5_XMM6,               // 26
    (PCODE)Store_XMM2_XMM3_XMM4_XMM5_XMM6_XMM7,          // 27
    (PCODE)0,                                            // 30
    (PCODE)0,                                            // 31
    (PCODE)0,                                            // 32
    (PCODE)Store_XMM3,                                   // 33
    (PCODE)Store_XMM3_XMM4,                              // 34
    (PCODE)Store_XMM3_XMM4_XMM5,                         // 35
    (PCODE)Store_XMM3_XMM4_XMM5_XMM6,                    // 36
    (PCODE)Store_XMM3_XMM4_XMM5_XMM6_XMM7,               // 37
    (PCODE)0,                                            // 40
    (PCODE)0,                                            // 41
    (PCODE)0,                                            // 42
    (PCODE)0,                                            // 43
    (PCODE)Store_XMM4,                                   // 44
    (PCODE)Store_XMM4_XMM5,                              // 45
    (PCODE)Store_XMM4_XMM5_XMM6,                         // 46
    (PCODE)Store_XMM4_XMM5_XMM6_XMM7,                    // 47
    (PCODE)0,                                            // 50
    (PCODE)0,                                            // 51
    (PCODE)0,                                            // 52
    (PCODE)0,                                            // 53
    (PCODE)0,                                            // 54
    (PCODE)Store_XMM5,                                   // 55
    (PCODE)Store_XMM5_XMM6,                              // 56
    (PCODE)Store_XMM5_XMM6_XMM7,                         // 57
    (PCODE)0,                                            // 60
    (PCODE)0,                                            // 61
    (PCODE)0,                                            // 62
    (PCODE)0,                                            // 63
    (PCODE)0,                                            // 64
    (PCODE)0,                                            // 65
    (PCODE)Store_XMM6,                                   // 66
    (PCODE)Store_XMM6_XMM7,                              // 67
    (PCODE)0,                                            // 70
    (PCODE)0,                                            // 71
    (PCODE)0,                                            // 72
    (PCODE)0,                                            // 73
    (PCODE)0,                                            // 74
    (PCODE)0,                                            // 75
    (PCODE)0,                                            // 76
    (PCODE)Store_XMM7                                    // 77
};

#endif // TARGET_WINDOWS

#endif // TARGET_AMD64

#ifdef TARGET_ARM64

extern "C" void Load_X0();
extern "C" void Load_X0_X1();
extern "C" void Load_X0_X1_X2();
extern "C" void Load_X0_X1_X2_X3();
extern "C" void Load_X0_X1_X2_X3_X4();
extern "C" void Load_X0_X1_X2_X3_X4_X5();
extern "C" void Load_X0_X1_X2_X3_X4_X5_X6();
extern "C" void Load_X0_X1_X2_X3_X4_X5_X6_X7();
extern "C" void Load_X1();
extern "C" void Load_X1_X2();
extern "C" void Load_X1_X2_X3();
extern "C" void Load_X1_X2_X3_X4();
extern "C" void Load_X1_X2_X3_X4_X5();
extern "C" void Load_X1_X2_X3_X4_X5_X6();
extern "C" void Load_X1_X2_X3_X4_X5_X6_X7();
extern "C" void Load_X2();
extern "C" void Load_X2_X3();
extern "C" void Load_X2_X3_X4();
extern "C" void Load_X2_X3_X4_X5();
extern "C" void Load_X2_X3_X4_X5_X6();
extern "C" void Load_X2_X3_X4_X5_X6_X7();
extern "C" void Load_X3();
extern "C" void Load_X3_X4();
extern "C" void Load_X3_X4_X5();
extern "C" void Load_X3_X4_X5_X6();
extern "C" void Load_X3_X4_X5_X6_X7();
extern "C" void Load_X4();
extern "C" void Load_X4_X5();
extern "C" void Load_X4_X5_X6();
extern "C" void Load_X4_X5_X6_X7();
extern "C" void Load_X5();
extern "C" void Load_X5_X6();
extern "C" void Load_X5_X6_X7();
extern "C" void Load_X6();
extern "C" void Load_X6_X7();
extern "C" void Load_X7();

extern "C" void Store_X0();
extern "C" void Store_X0_X1();
extern "C" void Store_X0_X1_X2();
extern "C" void Store_X0_X1_X2_X3();
extern "C" void Store_X0_X1_X2_X3_X4();
extern "C" void Store_X0_X1_X2_X3_X4_X5();
extern "C" void Store_X0_X1_X2_X3_X4_X5_X6();
extern "C" void Store_X0_X1_X2_X3_X4_X5_X6_X7();
extern "C" void Store_X1();
extern "C" void Store_X1_X2();
extern "C" void Store_X1_X2_X3();
extern "C" void Store_X1_X2_X3_X4();
extern "C" void Store_X1_X2_X3_X4_X5();
extern "C" void Store_X1_X2_X3_X4_X5_X6();
extern "C" void Store_X1_X2_X3_X4_X5_X6_X7();
extern "C" void Store_X2();
extern "C" void Store_X2_X3();
extern "C" void Store_X2_X3_X4();
extern "C" void Store_X2_X3_X4_X5();
extern "C" void Store_X2_X3_X4_X5_X6();
extern "C" void Store_X2_X3_X4_X5_X6_X7();
extern "C" void Store_X3();
extern "C" void Store_X3_X4();
extern "C" void Store_X3_X4_X5();
extern "C" void Store_X3_X4_X5_X6();
extern "C" void Store_X3_X4_X5_X6_X7();
extern "C" void Store_X4();
extern "C" void Store_X4_X5();
extern "C" void Store_X4_X5_X6();
extern "C" void Store_X4_X5_X6_X7();
extern "C" void Store_X5();
extern "C" void Store_X5_X6();
extern "C" void Store_X5_X6_X7();
extern "C" void Store_X6();
extern "C" void Store_X6_X7();
extern "C" void Store_X7();

extern "C" void Load_Ref_X0();
extern "C" void Load_Ref_X1();
extern "C" void Load_Ref_X2();
extern "C" void Load_Ref_X3();
extern "C" void Load_Ref_X4();
extern "C" void Load_Ref_X5();
extern "C" void Load_Ref_X6();
extern "C" void Load_Ref_X7();

extern "C" void Store_Ref_X0();
extern "C" void Store_Ref_X1();
extern "C" void Store_Ref_X2();
extern "C" void Store_Ref_X3();
extern "C" void Store_Ref_X4();
extern "C" void Store_Ref_X5();
extern "C" void Store_Ref_X6();
extern "C" void Store_Ref_X7();

PCODE GPRegsRoutines[] =
{
    (PCODE)Load_X0,                         // 00
    (PCODE)Load_X0_X1,                      // 01
    (PCODE)Load_X0_X1_X2,                   // 02
    (PCODE)Load_X0_X1_X2_X3,                // 03
    (PCODE)Load_X0_X1_X2_X3_X4,             // 04
    (PCODE)Load_X0_X1_X2_X3_X4_X5,          // 05
    (PCODE)Load_X0_X1_X2_X3_X4_X5_X6,       // 06
    (PCODE)Load_X0_X1_X2_X3_X4_X5_X6_X7,    // 07
    (PCODE)0,                               // 10
    (PCODE)Load_X1,                         // 11
    (PCODE)Load_X1_X2,                      // 12
    (PCODE)Load_X1_X2_X3,                   // 13
    (PCODE)Load_X1_X2_X3_X4,                // 14
    (PCODE)Load_X1_X2_X3_X4_X5,             // 15
    (PCODE)Load_X1_X2_X3_X4_X5_X6,          // 16
    (PCODE)Load_X1_X2_X3_X4_X5_X6_X7,       // 17
    (PCODE)0,                               // 20
    (PCODE)0,                               // 21
    (PCODE)Load_X2,                         // 22
    (PCODE)Load_X2_X3,                      // 23
    (PCODE)Load_X2_X3_X4,                   // 24
    (PCODE)Load_X2_X3_X4_X5,                // 25
    (PCODE)Load_X2_X3_X4_X5_X6,             // 26
    (PCODE)Load_X2_X3_X4_X5_X6_X7,          // 27
    (PCODE)0,                               // 30
    (PCODE)0,                               // 31
    (PCODE)0,                               // 32
    (PCODE)Load_X3,                         // 33
    (PCODE)Load_X3_X4,                      // 34
    (PCODE)Load_X3_X4_X5,                   // 35
    (PCODE)Load_X3_X4_X5_X6,                // 36
    (PCODE)Load_X3_X4_X5_X6_X7,             // 37
    (PCODE)0,                               // 40
    (PCODE)0,                               // 41
    (PCODE)0,                               // 42
    (PCODE)0,                               // 43
    (PCODE)Load_X4,                         // 44
    (PCODE)Load_X4_X5,                      // 45
    (PCODE)Load_X4_X5_X6,                   // 46
    (PCODE)Load_X4_X5_X6_X7,                // 47
    (PCODE)0,                               // 50
    (PCODE)0,                               // 51
    (PCODE)0,                               // 52
    (PCODE)0,                               // 53
    (PCODE)0,                               // 54
    (PCODE)Load_X5,                         // 55
    (PCODE)Load_X5_X6,                      // 56
    (PCODE)Load_X5_X6_X7,                   // 57
    (PCODE)0,                               // 60
    (PCODE)0,                               // 61
    (PCODE)0,                               // 62
    (PCODE)0,                               // 63
    (PCODE)0,                               // 64
    (PCODE)0,                               // 65
    (PCODE)Load_X6,                         // 66
    (PCODE)Load_X6_X7,                      // 67
    (PCODE)0,                               // 70
    (PCODE)0,                               // 71
    (PCODE)0,                               // 72
    (PCODE)0,                               // 73
    (PCODE)0,                               // 74
    (PCODE)0,                               // 75
    (PCODE)0,                               // 76
    (PCODE)Load_X7                          // 77
};

PCODE GPRegsStoreRoutines[] =
{
    (PCODE)Store_X0,                         // 00
    (PCODE)Store_X0_X1,                      // 01
    (PCODE)Store_X0_X1_X2,                   // 02
    (PCODE)Store_X0_X1_X2_X3,                // 03
    (PCODE)Store_X0_X1_X2_X3_X4,             // 04
    (PCODE)Store_X0_X1_X2_X3_X4_X5,          // 05
    (PCODE)Store_X0_X1_X2_X3_X4_X5_X6,       // 06
    (PCODE)Store_X0_X1_X2_X3_X4_X5_X6_X7,    // 07
    (PCODE)0,                                // 10
    (PCODE)Store_X1,                         // 11
    (PCODE)Store_X1_X2,                      // 12
    (PCODE)Store_X1_X2_X3,                   // 13
    (PCODE)Store_X1_X2_X3_X4,                // 14
    (PCODE)Store_X1_X2_X3_X4_X5,             // 15
    (PCODE)Store_X1_X2_X3_X4_X5_X6,          // 16
    (PCODE)Store_X1_X2_X3_X4_X5_X6_X7,       // 17
    (PCODE)0,                                // 20
    (PCODE)0,                                // 21
    (PCODE)Store_X2,                         // 22
    (PCODE)Store_X2_X3,                      // 23
    (PCODE)Store_X2_X3_X4,                   // 24
    (PCODE)Store_X2_X3_X4_X5,                // 25
    (PCODE)Store_X2_X3_X4_X5_X6,             // 26
    (PCODE)Store_X2_X3_X4_X5_X6_X7,          // 27
    (PCODE)0,                                // 30
    (PCODE)0,                                // 31
    (PCODE)0,                                // 32
    (PCODE)Store_X3,                         // 33
    (PCODE)Store_X3_X4,                      // 34
    (PCODE)Store_X3_X4_X5,                   // 35
    (PCODE)Store_X3_X4_X5_X6,                // 36
    (PCODE)Store_X3_X4_X5_X6_X7,             // 37
    (PCODE)0,                                // 40
    (PCODE)0,                                // 41
    (PCODE)0,                                // 42
    (PCODE)0,                                // 43
    (PCODE)Store_X4,                         // 44
    (PCODE)Store_X4_X5,                      // 45
    (PCODE)Store_X4_X5_X6,                   // 46
    (PCODE)Store_X4_X5_X6_X7,                // 47
    (PCODE)0,                                // 50
    (PCODE)0,                                // 51
    (PCODE)0,                                // 52
    (PCODE)0,                                // 53
    (PCODE)0,                                // 54
    (PCODE)Store_X5,                         // 55
    (PCODE)Store_X5_X6,                      // 56
    (PCODE)Store_X5_X6_X7,                   // 57
    (PCODE)0,                                // 60
    (PCODE)0,                                // 61
    (PCODE)0,                                // 62
    (PCODE)0,                                // 63
    (PCODE)0,                                // 64
    (PCODE)0,                                // 65
    (PCODE)Store_X6,                         // 66
    (PCODE)Store_X6_X7,                      // 67
    (PCODE)0,                                // 70
    (PCODE)0,                                // 71
    (PCODE)0,                                // 72
    (PCODE)0,                                // 73
    (PCODE)0,                                // 74
    (PCODE)0,                                // 75
    (PCODE)0,                                // 76
    (PCODE)Store_X7                          // 77
};

PCODE GPRegsRefRoutines[] =
{
    (PCODE)Load_Ref_X0,        // 0
    (PCODE)Load_Ref_X1,        // 1
    (PCODE)Load_Ref_X2,        // 2
    (PCODE)Load_Ref_X3,        // 3
    (PCODE)Load_Ref_X4,        // 4
    (PCODE)Load_Ref_X5,        // 5
    (PCODE)Load_Ref_X6,        // 6
    (PCODE)Load_Ref_X7         // 7
};

PCODE GPRegsRefStoreRoutines[] =
{
    (PCODE)Store_Ref_X0,        // 0
    (PCODE)Store_Ref_X1,        // 1
    (PCODE)Store_Ref_X2,        // 2
    (PCODE)Store_Ref_X3,        // 3
    (PCODE)Store_Ref_X4,        // 4
    (PCODE)Store_Ref_X5,        // 5
    (PCODE)Store_Ref_X6,        // 6
    (PCODE)Store_Ref_X7         // 7
};

extern "C" void Load_D0();
extern "C" void Load_D0_D1();
extern "C" void Load_D0_D1_D2();
extern "C" void Load_D0_D1_D2_D3();
extern "C" void Load_D0_D1_D2_D3_D4();
extern "C" void Load_D0_D1_D2_D3_D4_D5();
extern "C" void Load_D0_D1_D2_D3_D4_D5_D6();
extern "C" void Load_D0_D1_D2_D3_D4_D5_D6_D7();
extern "C" void Load_D1();
extern "C" void Load_D1_D2();
extern "C" void Load_D1_D2_D3();
extern "C" void Load_D1_D2_D3_D4();
extern "C" void Load_D1_D2_D3_D4_D5();
extern "C" void Load_D1_D2_D3_D4_D5_D6();
extern "C" void Load_D1_D2_D3_D4_D5_D6_D7();
extern "C" void Load_D2();
extern "C" void Load_D2_D3();
extern "C" void Load_D2_D3_D4();
extern "C" void Load_D2_D3_D4_D5();
extern "C" void Load_D2_D3_D4_D5_D6();
extern "C" void Load_D2_D3_D4_D5_D6_D7();
extern "C" void Load_D3();
extern "C" void Load_D3_D4();
extern "C" void Load_D3_D4_D5();
extern "C" void Load_D3_D4_D5_D6();
extern "C" void Load_D3_D4_D5_D6_D7();
extern "C" void Load_D4();
extern "C" void Load_D4_D5();
extern "C" void Load_D4_D5_D6();
extern "C" void Load_D4_D5_D6_D7();
extern "C" void Load_D5();
extern "C" void Load_D5_D6();
extern "C" void Load_D5_D6_D7();
extern "C" void Load_D6();
extern "C" void Load_D6_D7();
extern "C" void Load_D7();

extern "C" void Store_D0();
extern "C" void Store_D0_D1();
extern "C" void Store_D0_D1_D2();
extern "C" void Store_D0_D1_D2_D3();
extern "C" void Store_D0_D1_D2_D3_D4();
extern "C" void Store_D0_D1_D2_D3_D4_D5();
extern "C" void Store_D0_D1_D2_D3_D4_D5_D6();
extern "C" void Store_D0_D1_D2_D3_D4_D5_D6_D7();
extern "C" void Store_D1();
extern "C" void Store_D1_D2();
extern "C" void Store_D1_D2_D3();
extern "C" void Store_D1_D2_D3_D4();
extern "C" void Store_D1_D2_D3_D4_D5();
extern "C" void Store_D1_D2_D3_D4_D5_D6();
extern "C" void Store_D1_D2_D3_D4_D5_D6_D7();
extern "C" void Store_D2();
extern "C" void Store_D2_D3();
extern "C" void Store_D2_D3_D4();
extern "C" void Store_D2_D3_D4_D5();
extern "C" void Store_D2_D3_D4_D5_D6();
extern "C" void Store_D2_D3_D4_D5_D6_D7();
extern "C" void Store_D3();
extern "C" void Store_D3_D4();
extern "C" void Store_D3_D4_D5();
extern "C" void Store_D3_D4_D5_D6();
extern "C" void Store_D3_D4_D5_D6_D7();
extern "C" void Store_D4();
extern "C" void Store_D4_D5();
extern "C" void Store_D4_D5_D6();
extern "C" void Store_D4_D5_D6_D7();
extern "C" void Store_D5();
extern "C" void Store_D5_D6();
extern "C" void Store_D5_D6_D7();
extern "C" void Store_D6();
extern "C" void Store_D6_D7();
extern "C" void Store_D7();

PCODE FPRegsStoreRoutines[] =
{
    (PCODE)Store_D0,                         // 00
    (PCODE)Store_D0_D1,                      // 01
    (PCODE)Store_D0_D1_D2,                   // 02
    (PCODE)Store_D0_D1_D2_D3,                // 03
    (PCODE)Store_D0_D1_D2_D3_D4,             // 04
    (PCODE)Store_D0_D1_D2_D3_D4_D5,          // 05
    (PCODE)Store_D0_D1_D2_D3_D4_D5_D6,       // 06
    (PCODE)Store_D0_D1_D2_D3_D4_D5_D6_D7,    // 07
    (PCODE)0,                                // 10
    (PCODE)Store_D1,                         // 11
    (PCODE)Store_D1_D2,                      // 12
    (PCODE)Store_D1_D2_D3,                   // 13
    (PCODE)Store_D1_D2_D3_D4,                // 14
    (PCODE)Store_D1_D2_D3_D4_D5,             // 15
    (PCODE)Store_D1_D2_D3_D4_D5_D6,          // 16
    (PCODE)Store_D1_D2_D3_D4_D5_D6_D7,       // 17
    (PCODE)0,                                // 20
    (PCODE)0,                                // 21
    (PCODE)Store_D2,                         // 22
    (PCODE)Store_D2_D3,                      // 23
    (PCODE)Store_D2_D3_D4,                   // 24
    (PCODE)Store_D2_D3_D4_D5,                // 25
    (PCODE)Store_D2_D3_D4_D5_D6,             // 26
    (PCODE)Store_D2_D3_D4_D5_D6_D7,          // 27
    (PCODE)0,                                // 30
    (PCODE)0,                                // 31
    (PCODE)0,                                // 32
    (PCODE)Store_D3,                         // 33
    (PCODE)Store_D3_D4,                      // 34
    (PCODE)Store_D3_D4_D5,                   // 35
    (PCODE)Store_D3_D4_D5_D6,                // 36
    (PCODE)Store_D3_D4_D5_D6_D7,             // 37
    (PCODE)0,                                // 40
    (PCODE)0,                                // 41
    (PCODE)0,                                // 42
    (PCODE)0,                                // 43
    (PCODE)Store_D4,                         // 44
    (PCODE)Store_D4_D5,                      // 45
    (PCODE)Store_D4_D5_D6,                   // 46
    (PCODE)Store_D4_D5_D6_D7,                // 47
    (PCODE)0,                                // 50
    (PCODE)0,                                // 51
    (PCODE)0,                                // 52
    (PCODE)0,                                // 53
    (PCODE)0,                                // 54
    (PCODE)Store_D5,                         // 55
    (PCODE)Store_D5_D6,                      // 56
    (PCODE)Store_D5_D6_D7,                   // 57
    (PCODE)0,                                // 60
    (PCODE)0,                                // 61
    (PCODE)0,                                // 62
    (PCODE)0,                                // 63
    (PCODE)0,                                // 64
    (PCODE)0,                                // 65
    (PCODE)Store_D6,                         // 66
    (PCODE)Store_D6_D7,                      // 67
    (PCODE)0,                                // 70
    (PCODE)0,                                // 71
    (PCODE)0,                                // 72
    (PCODE)0,                                // 73
    (PCODE)0,                                // 74
    (PCODE)0,                                // 75
    (PCODE)0,                                // 76
    (PCODE)Store_D7                          // 77
};

PCODE FPRegsRoutines[] =
{
    (PCODE)Load_D0,                         // 00
    (PCODE)Load_D0_D1,                      // 01
    (PCODE)Load_D0_D1_D2,                   // 02
    (PCODE)Load_D0_D1_D2_D3,                // 03
    (PCODE)Load_D0_D1_D2_D3_D4,             // 04
    (PCODE)Load_D0_D1_D2_D3_D4_D5,          // 05
    (PCODE)Load_D0_D1_D2_D3_D4_D5_D6,       // 06
    (PCODE)Load_D0_D1_D2_D3_D4_D5_D6_D7,    // 07
    (PCODE)0,                               // 10
    (PCODE)Load_D1,                         // 11
    (PCODE)Load_D1_D2,                      // 12
    (PCODE)Load_D1_D2_D3,                   // 13
    (PCODE)Load_D1_D2_D3_D4,                // 14
    (PCODE)Load_D1_D2_D3_D4_D5,             // 15
    (PCODE)Load_D1_D2_D3_D4_D5_D6,          // 16
    (PCODE)Load_D1_D2_D3_D4_D5_D6_D7,       // 17
    (PCODE)0,                               // 20
    (PCODE)0,                               // 21
    (PCODE)Load_D2,                         // 22
    (PCODE)Load_D2_D3,                      // 23
    (PCODE)Load_D2_D3_D4,                   // 24
    (PCODE)Load_D2_D3_D4_D5,                // 25
    (PCODE)Load_D2_D3_D4_D5_D6,             // 26
    (PCODE)Load_D2_D3_D4_D5_D6_D7,          // 27
    (PCODE)0,                               // 30
    (PCODE)0,                               // 31
    (PCODE)0,                               // 32
    (PCODE)Load_D3,                         // 33
    (PCODE)Load_D3_D4,                      // 34
    (PCODE)Load_D3_D4_D5,                   // 35
    (PCODE)Load_D3_D4_D5_D6,                // 36
    (PCODE)Load_D3_D4_D5_D6_D7,             // 37
    (PCODE)0,                               // 40
    (PCODE)0,                               // 41
    (PCODE)0,                               // 42
    (PCODE)0,                               // 43
    (PCODE)Load_D4,                         // 44
    (PCODE)Load_D4_D5,                      // 45
    (PCODE)Load_D4_D5_D6,                   // 46
    (PCODE)Load_D4_D5_D6_D7,                // 47
    (PCODE)0,                               // 50
    (PCODE)0,                               // 51
    (PCODE)0,                               // 52
    (PCODE)0,                               // 53
    (PCODE)0,                               // 54
    (PCODE)Load_D5,                         // 55
    (PCODE)Load_D5_D6,                      // 56
    (PCODE)Load_D5_D6_D7,                   // 57
    (PCODE)0,                               // 60
    (PCODE)0,                               // 61
    (PCODE)0,                               // 62
    (PCODE)0,                               // 63
    (PCODE)0,                               // 64
    (PCODE)0,                               // 65
    (PCODE)Load_D6,                         // 66
    (PCODE)Load_D6_D7,                      // 67
    (PCODE)0,                               // 70
    (PCODE)0,                               // 71
    (PCODE)0,                               // 72
    (PCODE)0,                               // 73
    (PCODE)0,                               // 74
    (PCODE)0,                               // 75
    (PCODE)0,                               // 76
    (PCODE)Load_D7                          // 77
};

#endif // TARGET_ARM64

#define LOG_COMPUTE_CALL_STUB 0

PCODE CallStubGenerator::GetStackRoutine()
{
#if LOG_COMPUTE_CALL_STUB
    printf("Load_Stack\n");
#endif
    return m_interpreterToNative ? (PCODE)Load_Stack : (PCODE)Store_Stack;
}

#if defined(TARGET_APPLE) && defined(TARGET_ARM64)
PCODE CallStubGenerator::GetStackRoutine_1B()
{
#if LOG_COMPUTE_CALL_STUB
    printf("GetStackRoutine_1B\n");
#endif
    return m_interpreterToNative ? (PCODE)Load_Stack_1B : (PCODE)Store_Stack_1B;
}

PCODE CallStubGenerator::GetStackRoutine_2B()
{
#if LOG_COMPUTE_CALL_STUB
    printf("GetStackRoutine_2B\n");
#endif
    return m_interpreterToNative ? (PCODE)Load_Stack_2B : (PCODE)Store_Stack_2B;
}

PCODE CallStubGenerator::GetStackRoutine_4B()
{
#if LOG_COMPUTE_CALL_STUB
    printf("GetStackRoutine_4B\n");
#endif
    return m_interpreterToNative ? (PCODE)Load_Stack_4B : (PCODE)Store_Stack_4B;
}
#endif // TARGET_APPLE && TARGET_ARM64

PCODE CallStubGenerator::GetGPRegRangeRoutine(int r1, int r2)
{
#if LOG_COMPUTE_CALL_STUB
    printf("GetGPRegRangeRoutine %d %d\n", r1, r2);
#endif

    int index = r1 * NUM_ARGUMENT_REGISTERS + r2;
    return m_interpreterToNative ? GPRegsRoutines[index] : GPRegsStoreRoutines[index];
}

#ifndef UNIX_AMD64_ABI
PCODE CallStubGenerator::GetGPRegRefRoutine(int r)
{
#if LOG_COMPUTE_CALL_STUB
    printf("GetGPRegRefRoutine %d\n", r);
#endif
    return m_interpreterToNative ? GPRegsRefRoutines[r] : GPRegsRefStoreRoutines[r];
}

#endif // UNIX_AMD64_ABI

PCODE CallStubGenerator::GetFPRegRangeRoutine(int x1, int x2)
{
#if LOG_COMPUTE_CALL_STUB
    printf("GetFPRegRangeRoutine %d %d\n", x1, x2);
#endif
    int index = x1 * NUM_FLOAT_ARGUMENT_REGISTERS + x2;
    return m_interpreterToNative ? FPRegsRoutines[index] : FPRegsStoreRoutines[index];
}

extern "C" void CallJittedMethodRetVoid(PCODE *routines, int8_t*pArgs, int8_t*pRet, int totalStackSize);
extern "C" void CallJittedMethodRetDouble(PCODE *routines, int8_t*pArgs, int8_t*pRet, int totalStackSize);
extern "C" void CallJittedMethodRetI8(PCODE *routines, int8_t*pArgs, int8_t*pRet, int totalStackSize);
extern "C" void InterpreterStubRetVoid();
extern "C" void InterpreterStubRetDouble();
extern "C" void InterpreterStubRetI8();

#if defined(TARGET_WINDOWS) && defined(TARGET_AMD64)
extern "C" void CallJittedMethodRetBuffRCX(PCODE *routines, int8_t*pArgs, int8_t*pRet, int totalStackSize);
extern "C" void CallJittedMethodRetBuffRDX(PCODE *routines, int8_t*pArgs, int8_t*pRet, int totalStackSize);
extern "C" void InterpreterStubRetBuffRCX();
extern "C" void InterpreterStubRetBuffRDX();
#else // TARGET_WINDOWS && TARGET_AMD64
extern "C" void CallJittedMethodRetBuff(PCODE *routines, int8_t*pArgs, int8_t*pRet, int totalStackSize);
extern "C" void InterpreterStubRetBuff();
#endif // TARGET_WINDOWS && TARGET_AMD64

#ifdef UNIX_AMD64_ABI
extern "C" void CallJittedMethodRetI8I8(PCODE *routines, int8_t*pArgs, int8_t*pRet, int totalStackSize);
extern "C" void CallJittedMethodRetI8Double(PCODE *routines, int8_t*pArgs, int8_t*pRet, int totalStackSize);
extern "C" void CallJittedMethodRetDoubleI8(PCODE *routines, int8_t*pArgs, int8_t*pRet, int totalStackSize);
extern "C" void CallJittedMethodRetDoubleDouble(PCODE *routines, int8_t*pArgs, int8_t*pRet, int totalStackSize);
extern "C" void InterpreterStubRetI8I8();
extern "C" void InterpreterStubRetI8Double();
extern "C" void InterpreterStubRetDoubleI8();
extern "C" void InterpreterStubRetDoubleDouble();
#endif

#ifdef TARGET_ARM64
extern "C" void CallJittedMethodRet2I8(PCODE *routines, int8_t*pArgs, int8_t*pRet, int totalStackSize);
extern "C" void CallJittedMethodRet2Double(PCODE *routines, int8_t*pArgs, int8_t*pRet, int totalStackSize);
extern "C" void CallJittedMethodRet3Double(PCODE *routines, int8_t*pArgs, int8_t*pRet, int totalStackSize);
extern "C" void CallJittedMethodRet4Double(PCODE *routines, int8_t*pArgs, int8_t*pRet, int totalStackSize);
extern "C" void CallJittedMethodRetFloat(PCODE *routines, int8_t*pArgs, int8_t*pRet, int totalStackSize);
extern "C" void CallJittedMethodRet2Float(PCODE *routines, int8_t*pArgs, int8_t*pRet, int totalStackSize);
extern "C" void CallJittedMethodRet3Float(PCODE *routines, int8_t*pArgs, int8_t*pRet, int totalStackSize);
extern "C" void CallJittedMethodRet4Float(PCODE *routines, int8_t*pArgs, int8_t*pRet, int totalStackSize);
extern "C" void InterpreterStubRet2I8();
extern "C" void InterpreterStubRet2Double();
extern "C" void InterpreterStubRet3Double();
extern "C" void InterpreterStubRet4Double();
extern "C" void InterpreterStubRetFloat();
extern "C" void InterpreterStubRet2Float();
extern "C" void InterpreterStubRet3Float();
extern "C" void InterpreterStubRet4Float();
#endif // TARGET_ARM64

#if LOG_COMPUTE_CALL_STUB
#define INVOKE_FUNCTION_PTR(functionPtrName) printf(#functionPtrName "\n"); return functionPtrName
#else
#define INVOKE_FUNCTION_PTR(functionPtrName) return functionPtrName
#endif

CallStubHeader::InvokeFunctionPtr CallStubGenerator::GetInvokeFunctionPtr(CallStubGenerator::ReturnType returnType)
{
    STANDARD_VM_CONTRACT;

    switch (returnType)
    {
        case ReturnTypeVoid:
            INVOKE_FUNCTION_PTR(CallJittedMethodRetVoid);
        case ReturnTypeDouble:
            INVOKE_FUNCTION_PTR(CallJittedMethodRetDouble);
        case ReturnTypeI8:
            INVOKE_FUNCTION_PTR(CallJittedMethodRetI8);
#if defined(TARGET_WINDOWS) && defined(TARGET_AMD64)
        case ReturnTypeBuffArg1:
            INVOKE_FUNCTION_PTR(CallJittedMethodRetBuffRCX);
        case ReturnTypeBuffArg2:
            INVOKE_FUNCTION_PTR(CallJittedMethodRetBuffRDX);
#else // TARGET_WINDOWS && TARGET_AMD64
        case ReturnTypeBuff:
            INVOKE_FUNCTION_PTR(CallJittedMethodRetBuff);
#endif // TARGET_WINDOWS && TARGET_AMD64
#ifdef UNIX_AMD64_ABI
        case ReturnTypeI8I8:
            INVOKE_FUNCTION_PTR(CallJittedMethodRetI8I8);
        case ReturnTypeI8Double:
            INVOKE_FUNCTION_PTR(CallJittedMethodRetI8Double);
        case ReturnTypeDoubleI8:
            INVOKE_FUNCTION_PTR(CallJittedMethodRetDoubleI8);
        case ReturnTypeDoubleDouble:
            INVOKE_FUNCTION_PTR(CallJittedMethodRetDoubleDouble);
#endif // UNIX_AMD64_ABI
#ifdef TARGET_ARM64
        case ReturnType2I8:
            INVOKE_FUNCTION_PTR(CallJittedMethodRet2I8);
        case ReturnType2Double:
            INVOKE_FUNCTION_PTR(CallJittedMethodRet2Double);
        case ReturnType3Double:
            INVOKE_FUNCTION_PTR(CallJittedMethodRet3Double);
        case ReturnType4Double:
            INVOKE_FUNCTION_PTR(CallJittedMethodRet4Double);
        case ReturnTypeFloat:
            INVOKE_FUNCTION_PTR(CallJittedMethodRetFloat);
        case ReturnType2Float:
            INVOKE_FUNCTION_PTR(CallJittedMethodRet2Float);
        case ReturnType3Float:
            INVOKE_FUNCTION_PTR(CallJittedMethodRet3Float);
        case ReturnType4Float:
            INVOKE_FUNCTION_PTR(CallJittedMethodRet4Float);
#endif // TARGET_ARM64
        default:
            _ASSERTE(!"Unexpected return type for interpreter stub");
            return NULL; // This should never happen, but just in case.
    }
}

#if LOG_COMPUTE_CALL_STUB
#define RETURN_TYPE_HANDLER(returnType) printf(#returnType "\n"); return (PCODE)returnType
#else
#define RETURN_TYPE_HANDLER(returnType) return (PCODE)returnType
#endif

PCODE CallStubGenerator::GetInterpreterReturnTypeHandler(CallStubGenerator::ReturnType returnType)
{
    STANDARD_VM_CONTRACT;

    switch (returnType)
    {
        case ReturnTypeVoid:
            RETURN_TYPE_HANDLER(InterpreterStubRetVoid);
        case ReturnTypeDouble:
            RETURN_TYPE_HANDLER(InterpreterStubRetDouble);
        case ReturnTypeI8:
            RETURN_TYPE_HANDLER(InterpreterStubRetI8);
#if defined(TARGET_WINDOWS) && defined(TARGET_AMD64)
        case ReturnTypeBuffArg1:
            RETURN_TYPE_HANDLER(InterpreterStubRetBuffRCX);
        case ReturnTypeBuffArg2:
            RETURN_TYPE_HANDLER(InterpreterStubRetBuffRDX);
#else // TARGET_WINDOWS && TARGET_AMD64
        case ReturnTypeBuff:
            RETURN_TYPE_HANDLER(InterpreterStubRetBuff);
#endif // TARGET_WINDOWS && TARGET_AMD64
#ifdef UNIX_AMD64_ABI
        case ReturnTypeI8I8:
            RETURN_TYPE_HANDLER(InterpreterStubRetI8I8);
        case ReturnTypeI8Double:
            RETURN_TYPE_HANDLER(InterpreterStubRetI8Double);
        case ReturnTypeDoubleI8:
            RETURN_TYPE_HANDLER(InterpreterStubRetDoubleI8);
        case ReturnTypeDoubleDouble:
            RETURN_TYPE_HANDLER(InterpreterStubRetDoubleDouble);
#endif // UNIX_AMD64_ABI
#ifdef TARGET_ARM64
        case ReturnType2I8:
            RETURN_TYPE_HANDLER(InterpreterStubRet2I8);
        case ReturnType2Double:
            RETURN_TYPE_HANDLER(InterpreterStubRet2Double);
        case ReturnType3Double:
            RETURN_TYPE_HANDLER(InterpreterStubRet3Double);
        case ReturnType4Double:
            RETURN_TYPE_HANDLER(InterpreterStubRet4Double);
        case ReturnTypeFloat:
            RETURN_TYPE_HANDLER(InterpreterStubRetFloat);
        case ReturnType2Float:
            RETURN_TYPE_HANDLER(InterpreterStubRet2Float);
        case ReturnType3Float:
            RETURN_TYPE_HANDLER(InterpreterStubRet3Float);
        case ReturnType4Float:
            RETURN_TYPE_HANDLER(InterpreterStubRet4Float);
#endif // TARGET_ARM64
        default:
            _ASSERTE(!"Unexpected return type for interpreter stub");
            return 0; // This should never happen, but just in case.
    }
}

// Generate the call stub for the given method.
// The returned call stub header must be freed by the caller using FreeCallStub.
CallStubHeader *CallStubGenerator::GenerateCallStub(MethodDesc *pMD, AllocMemTracker *pamTracker, bool interpreterToNative)
{
    STANDARD_VM_CONTRACT;

    // String constructors are special cases, and have a special calling convention that is associated with the actual function executed (which is a static function with no this parameter)
    if (pMD->GetMethodTable()->IsString() && pMD->IsCtor())
    {
        _ASSERTE(pMD->IsFCall());
        MethodDesc *pMDActualImplementation = NonVirtualEntry2MethodDesc(ECall::GetFCallImpl(pMD));
        _ASSERTE(pMDActualImplementation != pMD);
        pMD = pMDActualImplementation;
    }

    _ASSERTE(pMD != NULL);

#if LOG_COMPUTE_CALL_STUB
    printf("GenerateCallStub interpreterToNative=%d\n", interpreterToNative ? 1 : 0);
#endif // LOG_COMPUTE_CALL_STUB
    m_interpreterToNative = interpreterToNative;

    MetaSig sig(pMD);
    // Allocate space for the routines. The size of the array is conservatively set to twice the number of arguments
    // plus one slot for the target pointer and reallocated to the real size at the end.
    size_t tempStorageSize = ComputeTempStorageSize(sig);
    PCODE *pRoutines = (PCODE*)alloca(tempStorageSize);
    memset(pRoutines, 0, tempStorageSize);

    ComputeCallStub(sig, pRoutines);

    LoaderAllocator *pLoaderAllocator = pMD->GetLoaderAllocator();
    S_SIZE_T finalStubSize(sizeof(CallStubHeader) + m_routineIndex * sizeof(PCODE));
    void *pHeaderStorage = pamTracker->Track(pLoaderAllocator->GetHighFrequencyHeap()->AllocMem(finalStubSize));

    CallStubHeader *pHeader = new (pHeaderStorage) CallStubHeader(m_routineIndex, pRoutines, ALIGN_UP(m_totalStackSize, STACK_ALIGN_SIZE), m_pInvokeFunction);

    return pHeader;
}

struct CachedCallStubKey
{
    CachedCallStubKey(int32_t hashCode, int numRoutines, PCODE *pRoutines, int totalStackSize, CallStubHeader::InvokeFunctionPtr pInvokeFunction)
     : HashCode(hashCode), NumRoutines(numRoutines), TotalStackSize(totalStackSize), Invoke(pInvokeFunction), Routines(pRoutines)
    {
    }

    bool operator==(const CachedCallStubKey& other) const
    {
        LIMITED_METHOD_CONTRACT;

        if (HashCode != other.HashCode || NumRoutines != other.NumRoutines || TotalStackSize != other.TotalStackSize || Invoke != other.Invoke)
            return false;

        for (int i = 0; i < NumRoutines; i++)
        {
            if (Routines[i] != other.Routines[i])
                return false;
        }
        return true;
    }

    const int32_t HashCode = 0;
    const int NumRoutines = 0;
    const int TotalStackSize = 0;
    const CallStubHeader::InvokeFunctionPtr Invoke = NULL; // Pointer to the invoke function
    const PCODE *Routines;
};

struct CachedCallStub
{
    CachedCallStub(int32_t hashCode, int numRoutines, PCODE *pRoutines, int totalStackSize, CallStubHeader::InvokeFunctionPtr pInvokeFunction) :
        HashCode(hashCode),
        Header(numRoutines, pRoutines, totalStackSize, pInvokeFunction)
    {
    }

    int32_t HashCode;
    CallStubHeader Header;

    CachedCallStubKey GetKey()
    {
        return CachedCallStubKey(
            HashCode,
            Header.NumRoutines,
            &Header.Routines[0],
            Header.TotalStackSize,
            Header.Invoke);
    }

    static COUNT_T Hash(const CachedCallStubKey& key)
    {
        LIMITED_METHOD_CONTRACT;
        return key.HashCode;
    }
};

static CrstStatic s_callStubCrst;

typedef  PtrSHashTraits<CachedCallStub, CachedCallStubKey> CallStubCacheTraits;

typedef SHash<CallStubCacheTraits> CallStubCacheHash;
static CallStubCacheHash* s_callStubCache;

void InitCallStubGenerator()
{
    STANDARD_VM_CONTRACT;

    s_callStubCrst.Init(CrstCallStubCache);
    s_callStubCache = new CallStubCacheHash;
}

CallStubHeader *CallStubGenerator::GenerateCallStubForSig(MetaSig &sig)
{
    STANDARD_VM_CONTRACT;

    // Allocate space for the routines. The size of the array is conservatively set to twice the number of arguments
    // plus one slot for the target pointer and reallocated to the real size at the end.
    size_t tempStorageSize = ComputeTempStorageSize(sig);
    PCODE *pRoutines = (PCODE*)alloca(ComputeTempStorageSize(sig));
    memset(pRoutines, 0, tempStorageSize);

    m_interpreterToNative = true; // We always generate the interpreter to native call stub here

    ComputeCallStub(sig, pRoutines);

    xxHash hashState;
    for (int i = 0; i < m_routineIndex; i++)
    {
        hashState.AddPointer((void*)pRoutines[i]);
    }
    hashState.Add(m_totalStackSize);
    hashState.AddPointer((void*)m_pInvokeFunction);

    CachedCallStubKey cachedHeaderKey(
        hashState.ToHashCode(),
        m_routineIndex,
        pRoutines,
        ALIGN_UP(m_totalStackSize, STACK_ALIGN_SIZE),
        m_pInvokeFunction);

    CrstHolder lockHolder(&s_callStubCrst);
    CachedCallStub *pCachedHeader = s_callStubCache->Lookup(cachedHeaderKey);
    if (pCachedHeader != NULL)
    {
        // The stub is already cached, return the cached header
#if LOG_COMPUTE_CALL_STUB
        printf("CallStubHeader at %p\n", &pCachedHeader->Header);
#endif // LOG_COMPUTE_CALL_STUB
        return &pCachedHeader->Header;
    }
    else
    {
        AllocMemTracker amTracker;
        // The stub is not cached, create a new header and add it to the cache
        // We only need to allocate the actual pRoutines array, and then we can just use the cachedHeader we already constructed
        size_t finalCachedCallStubSize = sizeof(CachedCallStub) + m_routineIndex * sizeof(PCODE);
        void* pHeaderStorage = amTracker.Track(SystemDomain::GetGlobalLoaderAllocator()->GetHighFrequencyHeap()->AllocMem(S_SIZE_T(finalCachedCallStubSize)));
        CachedCallStub *pHeader = new (pHeaderStorage) CachedCallStub(cachedHeaderKey.HashCode, m_routineIndex, pRoutines, ALIGN_UP(m_totalStackSize, STACK_ALIGN_SIZE), m_pInvokeFunction);
        s_callStubCache->Add(pHeader);
        amTracker.SuppressRelease();

        _ASSERTE(s_callStubCache->Lookup(cachedHeaderKey) == pHeader);
#if LOG_COMPUTE_CALL_STUB
        printf("CallStubHeader at %p\n", &pHeader->Header);
#endif // LOG_COMPUTE_CALL_STUB
        return &pHeader->Header;
    }
};

void CallStubGenerator::ComputeCallStub(MetaSig &sig, PCODE *pRoutines)
{

    ArgIterator argIt(&sig);

    m_r1 = NoRange; // indicates that there is no active range of general purpose registers
    m_r2 = 0;
    m_x1 = NoRange; // indicates that there is no active range of FP registers
    m_x2 = 0;
    m_s1 = NoRange; // indicates that there is no active range of stack arguments
    m_s2 = 0;
    m_routineIndex = 0;
    m_totalStackSize = 0;
#if LOG_COMPUTE_CALL_STUB
    printf("ComputeCallStub\n");
#endif
    int numArgs = sig.NumFixedArgs() + (sig.HasThis() ? 1 : 0);

    if (argIt.HasThis())
    {
#if LOG_COMPUTE_CALL_STUB
        printf("HasThis\n");
#endif
        // The "this" argument register is not enumerated by the arg iterator, so
        // we need to "inject" it here.
#if defined(TARGET_WINDOWS) && defined(TARGET_AMD64)
        if (argIt.HasRetBuffArg())
        {
#if LOG_COMPUTE_CALL_STUB
            printf("argIt.HasRetBuffArg() on WINDOWS AMD64\n");
#endif
            // The return buffer on Windows AMD64 is passed in the first argument register, so the
            // "this" argument is be passed in the second argument register.
            m_r1 = 1;
        }
        else
#endif // TARGET_WINDOWS && TARGET_AMD64
        {
            // The "this" pointer is passed in the first argument register.
            m_r1 = 0;
        }
    }

    if (argIt.HasParamType())
    {
#if LOG_COMPUTE_CALL_STUB
            printf("argIt.HasParamType\n");
#endif
        // In the Interpreter calling convention the argument after the "this" pointer is the parameter type
        ArgLocDesc paramArgLocDesc;
        argIt.GetParamTypeLoc(&paramArgLocDesc);
        ProcessArgument(NULL, paramArgLocDesc, pRoutines);
    }

    int ofs;
    while ((ofs = argIt.GetNextOffset()) != TransitionBlock::InvalidOffset)
    {
#if LOG_COMPUTE_CALL_STUB
        printf("Next argument\n");
#endif
        ArgLocDesc argLocDesc;
        argIt.GetArgLoc(ofs, &argLocDesc);

#ifdef UNIX_AMD64_ABI
        if (argIt.GetArgLocDescForStructInRegs() != NULL)
        {
            TypeHandle argTypeHandle;
            CorElementType corType = argIt.GetArgType(&argTypeHandle);
            _ASSERTE(corType == ELEMENT_TYPE_VALUETYPE);

            MethodTable *pMT = argTypeHandle.AsMethodTable();
            EEClass *pEEClass = pMT->GetClass();
            int numEightBytes = pEEClass->GetNumberEightBytes();
            for (int i = 0; i < numEightBytes; i++)
            {
                ArgLocDesc argLocDescEightByte = {};
                SystemVClassificationType eightByteType = pEEClass->GetEightByteClassification(i);
                switch (eightByteType)
                {
                    case SystemVClassificationTypeInteger:
                    case SystemVClassificationTypeIntegerReference:
                    case SystemVClassificationTypeIntegerByRef:
                    {
                        if (argLocDesc.m_cGenReg != 0)
                        {
                            argLocDescEightByte.m_cGenReg = 1;
                            argLocDescEightByte.m_idxGenReg = argLocDesc.m_idxGenReg++;
                        }
                        else
                        {
                            argLocDescEightByte.m_byteStackSize = 8;
                            argLocDescEightByte.m_byteStackIndex = argLocDesc.m_byteStackIndex;
                            argLocDesc.m_byteStackIndex += 8;
                        }
                        break;
                    }
                    case SystemVClassificationTypeSSE:
                    {
                        if (argLocDesc.m_cFloatReg != 0)
                        {
                            argLocDescEightByte.m_cFloatReg = 1;
                            argLocDescEightByte.m_idxFloatReg = argLocDesc.m_idxFloatReg++;
                        }
                        else
                        {
                            argLocDescEightByte.m_byteStackSize = 8;
                            argLocDescEightByte.m_byteStackIndex = argLocDesc.m_byteStackIndex;
                            argLocDesc.m_byteStackIndex += 8;
                        }
                        break;
                    }
                    default:
                        assert(!"Unhandled systemv classification for argument in GenerateCallStub");
                        break;
                }
                ProcessArgument(&argIt, argLocDescEightByte, pRoutines);
            }
        }
        else
#endif // UNIX_AMD64_ABI
        {
            ProcessArgument(&argIt, argLocDesc, pRoutines);
        }
    }

    // All arguments were processed, but there is likely a pending ranges to store.
    // Process such a range if any.
    if (m_r1 != NoRange)
    {
        pRoutines[m_routineIndex++] = GetGPRegRangeRoutine(m_r1, m_r2);
    }
    else if (m_x1 != NoRange)
    {
        pRoutines[m_routineIndex++] = GetFPRegRangeRoutine(m_x1, m_x2);
    }
    else if (m_s1 != NoRange)
    {
        m_totalStackSize += m_s2 - m_s1 + 1;
        pRoutines[m_routineIndex++] = GetStackRoutine();
        pRoutines[m_routineIndex++] = ((int64_t)(m_s2 - m_s1 + 1) << 32) | m_s1;
    }

    ReturnType returnType = GetReturnType(&argIt);

    if (m_interpreterToNative)
    {
        m_pInvokeFunction = GetInvokeFunctionPtr(returnType);
        m_routineIndex++; // Reserve one extra slot for the target method pointer
    }
    else
    {
        pRoutines[m_routineIndex++] = GetInterpreterReturnTypeHandler(returnType);
    }
}

// Process the argument described by argLocDesc. This function is called for each argument in the method signature.
// It updates the ranges of registers and emits entries into the routines array at discontinuities.
void CallStubGenerator::ProcessArgument(ArgIterator *pArgIt, ArgLocDesc& argLocDesc, PCODE *pRoutines)
{
    LIMITED_METHOD_CONTRACT;

    // Check if we have a range of registers or stack arguments that we need to store because the current argument
    // terminates it.
    if ((argLocDesc.m_cGenReg == 0) && (m_r1 != NoRange))
    {
        // No GP register is used to pass the current argument, but we already have a range of GP registers,
        // store the routine for the range
        pRoutines[m_routineIndex++] = GetGPRegRangeRoutine(m_r1, m_r2);
        m_r1 = NoRange;
    }
    else if (((argLocDesc.m_cFloatReg == 0)) && (m_x1 != NoRange))
    {
        // No floating point register is used to pass the current argument, but we already have a range of FP registers,
        // store the routine for the range
        pRoutines[m_routineIndex++] = GetFPRegRangeRoutine(m_x1, m_x2);
        m_x1 = NoRange;
    }
    else if ((argLocDesc.m_byteStackSize == 0) && (m_s1 != NoRange))
    {
        // No stack argument is used to pass the current argument, but we already have a range of stack arguments,
        // store the routine for the range
        m_totalStackSize += m_s2 - m_s1 + 1;
        pRoutines[m_routineIndex++] = GetStackRoutine();
        pRoutines[m_routineIndex++] = ((int64_t)(m_s2 - m_s1 + 1) << 32) | m_s1;
        m_s1 = NoRange;
    }

    if (argLocDesc.m_cGenReg != 0)
    {
#if LOG_COMPUTE_CALL_STUB
        printf("m_cGenReg=%d\n", (int)argLocDesc.m_cGenReg);
#endif // LOG_COMPUTE_CALL_STUB
        if (m_r1 == NoRange) // No active range yet
        {
            // Start a new range
            m_r1 = argLocDesc.m_idxGenReg;
            m_r2 = m_r1 + argLocDesc.m_cGenReg - 1;
        }
        else if (argLocDesc.m_idxGenReg == m_r2 + 1 && (!pArgIt || !pArgIt->IsArgPassedByRef()))
        {
            // Extend an existing range, but only if the argument is not passed by reference.
            // Arguments passed by reference are handled separately, because the interpreter stores the value types on its stack by value.
            m_r2 += argLocDesc.m_cGenReg;
        }
        else
        {
            // Discontinuous range - store a routine for the current and start a new one
            pRoutines[m_routineIndex++] = GetGPRegRangeRoutine(m_r1, m_r2);
            m_r1 = argLocDesc.m_idxGenReg;
            m_r2 = m_r1 + argLocDesc.m_cGenReg - 1;
        }
    }

    if (argLocDesc.m_cFloatReg != 0)
    {
#if LOG_COMPUTE_CALL_STUB
        printf("m_cFloatReg=%d\n", (int)argLocDesc.m_cFloatReg);
#endif // LOG_COMPUTE_CALL_STUB
        if (m_x1 == NoRange) // No active range yet
        {
            // Start a new range
            m_x1 = argLocDesc.m_idxFloatReg;
            m_x2 = m_x1 + argLocDesc.m_cFloatReg - 1;
        }
        else if (argLocDesc.m_idxFloatReg == m_x2 + 1)
        {
            // Extend an existing range
            m_x2 += argLocDesc.m_cFloatReg;
        }
        else
        {
            // Discontinuous range - store a routine for the current and start a new one
            pRoutines[m_routineIndex++] = GetFPRegRangeRoutine(m_x1, m_x2);
            m_x1 = argLocDesc.m_idxFloatReg;
            m_x2 = m_x1 + argLocDesc.m_cFloatReg - 1;
        }
    }

    if (argLocDesc.m_byteStackSize != 0)
    {
#if LOG_COMPUTE_CALL_STUB
        printf("m_byteStackSize=%d\n", (int)argLocDesc.m_byteStackSize);
#endif // LOG_COMPUTE_CALL_STUB
        if (m_s1 == NoRange) // No active range yet
        {
            // Start a new range
            m_s1 = argLocDesc.m_byteStackIndex;
            m_s2 = m_s1 + argLocDesc.m_byteStackSize - 1;
        }
        else if ((argLocDesc.m_byteStackIndex == m_s2 + 1) && (argLocDesc.m_byteStackSize >= 8))
        {
            // Extend an existing range, but only if the argument is at least pointer size large.
            // The only case when this is not true is on Apple ARM64 OSes where primitive type smaller
            // than 8 bytes are passed on the stack in a packed manner. We process such arguments one by
            // one to avoid explosion of the number of pRoutines.
            m_s2 += argLocDesc.m_byteStackSize;
        }
        else
        {
            // Discontinuous range - store a routine for the current and start a new one
            m_totalStackSize += m_s2 - m_s1 + 1;
            pRoutines[m_routineIndex++] = GetStackRoutine();
            pRoutines[m_routineIndex++] = ((int64_t)(m_s2 - m_s1 + 1) << 32) | m_s1;
            m_s1 = argLocDesc.m_byteStackIndex;
            m_s2 = m_s1 + argLocDesc.m_byteStackSize - 1;
        }

#if defined(TARGET_APPLE) && defined(TARGET_ARM64)
        // Process primitive types smaller than 8 bytes separately on Apple ARM64
        if (argLocDesc.m_byteStackSize < 8)
        {
            switch (argLocDesc.m_byteStackSize)
            {
                case 1:
                    pRoutines[m_routineIndex++] = GetStackRoutine_1B();
                    break;
                case 2:
                    pRoutines[m_routineIndex++] = GetStackRoutine_2B();
                    break;
                case 4:
                    pRoutines[m_routineIndex++] = GetStackRoutine_4B();
                    break;
                default:
                    _ASSERTE(!"Unexpected stack argument size");
                    break;
            }
            pRoutines[m_routineIndex++] = m_s1;
            m_s1 = NoRange;
        }
#endif // TARGET_APPLE && TARGET_ARM64
    }

#ifndef UNIX_AMD64_ABI
    // Arguments passed by reference are handled separately, because the interpreter stores the value types on its stack by value.
    // So the argument loading routine needs to load the address of the argument. To avoid explosion of number of the routines,
    // we always process single argument passed by reference using single routine.
    if (pArgIt != NULL && pArgIt->IsArgPassedByRef())
    {
        _ASSERTE(argLocDesc.m_cGenReg == 1);
        pRoutines[m_routineIndex++] = GetGPRegRefRoutine(argLocDesc.m_idxGenReg);
        pRoutines[m_routineIndex++] = pArgIt->GetArgSize();
        m_r1 = NoRange;
    }
#endif // UNIX_AMD64_ABI
}

CallStubGenerator::ReturnType CallStubGenerator::GetReturnType(ArgIterator *pArgIt)
{
    if (pArgIt->HasRetBuffArg())
    {
#if defined(TARGET_WINDOWS) && defined(TARGET_AMD64)
        if (pArgIt->HasThis())
        {
            return ReturnTypeBuffArg2;
        }
        else
        {
            return ReturnTypeBuffArg1;
        }
#else
        return ReturnTypeBuff;
#endif        
    }
    else
    {
        TypeHandle thReturnValueType;
        CorElementType thReturnType = pArgIt->GetSig()->GetReturnTypeNormalized(&thReturnValueType);

        switch (thReturnType)
        {
            case ELEMENT_TYPE_BOOLEAN:
            case ELEMENT_TYPE_CHAR:
            case ELEMENT_TYPE_I1:
            case ELEMENT_TYPE_U1:
            case ELEMENT_TYPE_I2:
            case ELEMENT_TYPE_U2:
            case ELEMENT_TYPE_I4:
            case ELEMENT_TYPE_U4:
            case ELEMENT_TYPE_I8:
            case ELEMENT_TYPE_U8:
            case ELEMENT_TYPE_I:
            case ELEMENT_TYPE_U:
            case ELEMENT_TYPE_CLASS:
            case ELEMENT_TYPE_OBJECT:
            case ELEMENT_TYPE_STRING:
            case ELEMENT_TYPE_PTR:
            case ELEMENT_TYPE_BYREF:
            case ELEMENT_TYPE_TYPEDBYREF:
            case ELEMENT_TYPE_ARRAY:
            case ELEMENT_TYPE_SZARRAY:
            case ELEMENT_TYPE_FNPTR:
                return ReturnTypeI8;
                break;
            case ELEMENT_TYPE_R4:
            case ELEMENT_TYPE_R8:
                return ReturnTypeDouble;
                break;
            case ELEMENT_TYPE_VOID:
                return ReturnTypeVoid;
                break;
            case ELEMENT_TYPE_VALUETYPE:
#ifdef TARGET_AMD64
#ifdef TARGET_WINDOWS
                // POD structs smaller than 64 bits are returned in rax
                return ReturnTypeI8;
#else // TARGET_WINDOWS
                if (thReturnValueType.AsMethodTable()->IsRegPassedStruct())
                {
                    UINT fpReturnSize = pArgIt->GetFPReturnSize();
                    if (fpReturnSize == 0)
                    {
                        return ReturnTypeI8;
                    }
                    else if (fpReturnSize == 8)
                    {
                        return ReturnTypeDouble;
                    }
                    else
                    {
                        _ASSERTE((fpReturnSize & 16) != 0);
                        // The fpReturnSize bits 0..1 have the following meaning:
                        // Bit 0 - the first 8 bytes of the struct is integer (0) or floating point (1)
                        // Bit 1 - the second 8 bytes of the struct is integer (0) or floating point (1)
                        switch (fpReturnSize & 0x3)
                        {
                            case 0:
                                return ReturnTypeI8I8;
                            case 1:
                                return ReturnTypeDoubleI8;
                            case 2:
                                return ReturnTypeI8Double;
                            case 3:
                                return ReturnTypeDoubleDouble;
                        }
                    }
                }
                else
                {
                    _ASSERTE(!"All value types that are not returnable structs in registers should be returned using return buffer");
                }
#endif // TARGET_WINDOWS
#elif TARGET_ARM64
                // HFA, HVA, POD structs smaller than 128 bits
                if (thReturnValueType.IsHFA())
                {
                    switch (thReturnValueType.GetHFAType())
                    {
                        case CORINFO_HFA_ELEM_FLOAT:
                            switch (thReturnValueType.GetSize())
                            {
                                case 4:
                                    return ReturnTypeFloat;
                                case 8:
                                    return ReturnType2Float;
                                case 12:
                                    return ReturnType3Float;
                                case 16:
                                    return ReturnType4Float;
                                default:
                                    _ASSERTE(!"Should not get here");
                                    break;
                            }
                            break;
                        case CORINFO_HFA_ELEM_DOUBLE:
                            switch (thReturnValueType.GetSize())
                            {
                                case 8:
                                    return ReturnTypeDouble;
                                case 16:
                                    return ReturnType2Double;
                                case 24:
                                    return ReturnType3Double;
                                case 32:
                                    return ReturnType4Double;
                                default:
                                    _ASSERTE(!"Should not get here");
                                    break;
                            }
                                break;
                        default:
                            _ASSERTE(!"HFA types other than float and double are not supported yet");
                            break;
                    }
                }
                else
                {
                    switch (thReturnValueType.GetSize())
                    {
                        case 1:
                        case 2:
                        case 4:
                        case 8:
                            return ReturnTypeI8;
                            break;
                        case 16:
                            return ReturnType2I8;
                        default:
                            _ASSERTE(!"The return types that are not HFA should be <= 16 bytes in size");
                            break;
                    }
                }
#else
                _ASSERTE(!"Struct returns by value are not supported yet");
#endif
                break;
            default:
                _ASSERTE(!"Unexpected return type");
                break;
        }
    }

    // We should never reach this spot
    return ReturnTypeVoid;
}

#endif // FEATURE_INTERPRETER
