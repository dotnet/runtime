// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifdef FEATURE_INTERPRETER

#include "callstubgenerator.h"

extern "C" void Load_Stack();

#if defined(TARGET_APPLE) && defined(TARGET_ARM64)
extern "C" void Load_Stack_1B();
extern "C" void Load_Stack_2B();
extern "C" void Load_Stack_4B();
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

PCODE GPRegsRefRoutines[] =
{
    (PCODE)Load_Ref_RCX,        // 0
    (PCODE)Load_Ref_RDX,        // 1
    (PCODE)Load_Ref_R8,         // 2
    (PCODE)Load_Ref_R9,         // 3
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

extern "C" void Load_Ref_X0();
extern "C" void Load_Ref_X1();
extern "C" void Load_Ref_X2();
extern "C" void Load_Ref_X3();
extern "C" void Load_Ref_X4();
extern "C" void Load_Ref_X5();
extern "C" void Load_Ref_X6();
extern "C" void Load_Ref_X7();


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
    (PCODE)Load_X3,                          // 33
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

PCODE GetGPRegRangeLoadRoutine(int r1, int r2)
{
    int index = r1 * NUM_ARGUMENT_REGISTERS + r2;
    return GPRegsRoutines[index];
}

#ifndef UNIX_AMD64_ABI
PCODE GetGPRegRefLoadRoutine(int r)
{
    return GPRegsRefRoutines[r];
}
#endif // UNIX_AMD64_ABI

PCODE GetFPRegRangeLoadRoutine(int x1, int x2)
{
    int index = x1 * NUM_FLOAT_ARGUMENT_REGISTERS + x2;
    return FPRegsRoutines[index];
}

extern "C" void CallJittedMethodRetVoid(PCODE *routines, int8_t*pArgs, int8_t*pRet, int totalStackSize);
extern "C" void CallJittedMethodRetDouble(PCODE *routines, int8_t*pArgs, int8_t*pRet, int totalStackSize);
extern "C" void CallJittedMethodRetI8(PCODE *routines, int8_t*pArgs, int8_t*pRet, int totalStackSize);
extern "C" void CallJittedMethodRetBuff(PCODE *routines, int8_t*pArgs, int8_t*pRet, int totalStackSize);

#ifdef UNIX_AMD64_ABI
extern "C" void CallJittedMethodRetI8I8(PCODE *routines, int8_t*pArgs, int8_t*pRet, int totalStackSize);
extern "C" void CallJittedMethodRetI8Double(PCODE *routines, int8_t*pArgs, int8_t*pRet, int totalStackSize);
extern "C" void CallJittedMethodRetDoubleI8(PCODE *routines, int8_t*pArgs, int8_t*pRet, int totalStackSize);
extern "C" void CallJittedMethodRetDoubleDouble(PCODE *routines, int8_t*pArgs, int8_t*pRet, int totalStackSize);
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
#endif // TARGET_ARM64

// Generate the call stub for the given method.
// The returned call stub header must be freed by the caller using FreeCallStub.
CallStubHeader *CallStubGenerator::GenerateCallStub(MethodDesc *pMD, AllocMemTracker *pamTracker)
{
    STANDARD_VM_CONTRACT;

    _ASSERTE(pMD != NULL);

    MetaSig sig(pMD);
    ArgIterator argIt(&sig);

    m_r1 = NoRange; // indicates that there is no active range of general purpose registers
    m_r2 = 0;
    m_x1 = NoRange; // indicates that there is no active range of FP registers
    m_x2 = 0;
    m_s1 = NoRange; // indicates that there is no active range of stack arguments
    m_s2 = 0;
    m_routineIndex = 0;
    m_totalStackSize = 0;

    int numArgs = sig.NumFixedArgs() + (sig.HasThis() ? 1 : 0);

    if (argIt.HasThis())
    {
        // The "this" argument register is not enumerated by the arg iterator, so
        // we need to "inject" it here.
#if defined(TARGET_WINDOWS) && defined(TARGET_AMD64)
        if (argIt.HasRetBuffArg())
        {
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

    // Allocate space for the routines. The size of the array is conservatively set to twice the number of arguments
    // plus one slot for the target pointer and reallocated to the real size at the end.
    PCODE *pRoutines = (PCODE*)alloca(sizeof(CallStubHeader) + (numArgs * 2 + 1) * sizeof(PCODE));

    int ofs;
    while ((ofs = argIt.GetNextOffset()) != TransitionBlock::InvalidOffset)
    {
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
                ProcessArgument(argIt, argLocDescEightByte, pRoutines);
            }
        }
        else
#endif // UNIX_AMD64_ABI
        {
            ProcessArgument(argIt, argLocDesc, pRoutines);
        }
    }

    // All arguments were processed, but there is likely a pending ranges to store.
    // Process such a range if any.
    if (m_r1 != NoRange)
    {
        pRoutines[m_routineIndex++] = GetGPRegRangeLoadRoutine(m_r1, m_r2);
    }
    else if (m_x1 != NoRange)
    {
        pRoutines[m_routineIndex++] = GetFPRegRangeLoadRoutine(m_x1, m_x2);
    }
    else if (m_s1 != NoRange)
    {
        m_totalStackSize += m_s2 - m_s1 + 1;
        pRoutines[m_routineIndex++] = (PCODE)Load_Stack;
        pRoutines[m_routineIndex++] = ((int64_t)(m_s2 - m_s1 + 1) << 32) | m_s1;
    }

    CallStubHeader::InvokeFunctionPtr pInvokeFunction = NULL;

    if (argIt.HasRetBuffArg())
    {
        pInvokeFunction = CallJittedMethodRetBuff;
    }
    else
    {
        TypeHandle thReturnValueType;
        CorElementType thReturnType = sig.GetReturnTypeNormalized(&thReturnValueType);

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
                pInvokeFunction = CallJittedMethodRetI8;
                break;
            case ELEMENT_TYPE_R4:
            case ELEMENT_TYPE_R8:
                pInvokeFunction = CallJittedMethodRetDouble;
                break;
            case ELEMENT_TYPE_VOID:
                pInvokeFunction = CallJittedMethodRetVoid;
                break;
            case ELEMENT_TYPE_VALUETYPE:
#ifdef TARGET_AMD64
#ifdef TARGET_WINDOWS
                if (thReturnValueType.AsMethodTable()->IsIntrinsicType())
                {
                    // E.g. Vector2
                    pInvokeFunction = CallJittedMethodRetDouble;
                }
                else
                {
                    // POD structs smaller than 64 bits are returned in rax
                    pInvokeFunction = CallJittedMethodRetI8;
                }
#else // TARGET_WINDOWS
                if (thReturnValueType.AsMethodTable()->IsRegPassedStruct())
                {
                    UINT fpReturnSize = argIt.GetFPReturnSize();
                    if (fpReturnSize == 0)
                    {
                        pInvokeFunction = CallJittedMethodRetI8;
                    }
                    else if (fpReturnSize == 8)
                    {
                        pInvokeFunction = CallJittedMethodRetDouble;
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
                                pInvokeFunction = CallJittedMethodRetI8I8;
                                break;
                            case 1:
                                pInvokeFunction = CallJittedMethodRetDoubleI8;
                                break;
                            case 2:
                                pInvokeFunction = CallJittedMethodRetI8Double;
                                break;
                            case 3:
                                pInvokeFunction = CallJittedMethodRetDoubleDouble;
                                break;
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
                                    pInvokeFunction = CallJittedMethodRetFloat;
                                    break;
                                case 8:
                                    pInvokeFunction = CallJittedMethodRet2Float;
                                    break;
                                case 12:
                                    pInvokeFunction = CallJittedMethodRet3Float;
                                    break;
                                case 16:
                                    pInvokeFunction = CallJittedMethodRet4Float;
                                    break;
                                default:
                                    _ASSERTE(!"Should not get here");
                                    break;
                            }
                            break;
                        case CORINFO_HFA_ELEM_DOUBLE:
                            switch (thReturnValueType.GetSize())
                            {
                                case 8:
                                    pInvokeFunction = CallJittedMethodRetDouble;
                                    break;
                                case 16:
                                    pInvokeFunction = CallJittedMethodRet2Double;
                                    break;
                                case 24:
                                    pInvokeFunction = CallJittedMethodRet3Double;
                                    break;
                                case 32:
                                    pInvokeFunction = CallJittedMethodRet4Double;
                                    break;
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
                            pInvokeFunction = CallJittedMethodRetI8;
                            break;
                        case 16:
                            pInvokeFunction = CallJittedMethodRet2I8;
                            break;
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

    m_routineIndex++; // Reserve one extra slot for the target method pointer

    LoaderAllocator *pLoaderAllocator = pMD->GetLoaderAllocator();
    S_SIZE_T finalStubSize(sizeof(CallStubHeader) + m_routineIndex * sizeof(PCODE));
    void *pHeaderStorage = pamTracker->Track(pLoaderAllocator->GetHighFrequencyHeap()->AllocMem(finalStubSize));

    CallStubHeader *pHeader = new (pHeaderStorage) CallStubHeader(m_routineIndex, pRoutines, ALIGN_UP(m_totalStackSize, STACK_ALIGN_SIZE), pInvokeFunction);

    return pHeader;
}

// Process the argument described by argLocDesc. This function is called for each argument in the method signature.
// It updates the ranges of registers and emits entries into the routines array at discontinuities.
void CallStubGenerator::ProcessArgument(ArgIterator& argIt, ArgLocDesc& argLocDesc, PCODE *pRoutines)
{
    LIMITED_METHOD_CONTRACT;

    // Check if we have a range of registers or stack arguments that we need to store because the current argument
    // terminates it.
    if ((argLocDesc.m_cGenReg == 0) && (m_r1 != NoRange))
    {
        // No GP register is used to pass the current argument, but we already have a range of GP registers,
        // store the routine for the range
        pRoutines[m_routineIndex++] = GetGPRegRangeLoadRoutine(m_r1, m_r2);
        m_r1 = NoRange;
    }
    else if (((argLocDesc.m_cFloatReg == 0)) && (m_x1 != NoRange))
    {
        // No floating point register is used to pass the current argument, but we already have a range of FP registers,
        // store the routine for the range
        pRoutines[m_routineIndex++] = GetFPRegRangeLoadRoutine(m_x1, m_x2);
        m_x1 = NoRange;
    }
    else if ((argLocDesc.m_byteStackSize == 0) && (m_s1 != NoRange))
    {
        // No stack argument is used to pass the current argument, but we already have a range of stack arguments,
        // store the routine for the range
        m_totalStackSize += m_s2 - m_s1 + 1;
        pRoutines[m_routineIndex++] = (PCODE)Load_Stack;
        pRoutines[m_routineIndex++] = ((int64_t)(m_s2 - m_s1 + 1) << 32) | m_s1;
        m_s1 = NoRange;
    }

    if (argLocDesc.m_cGenReg != 0)
    {
        if (m_r1 == NoRange) // No active range yet
        {
            // Start a new range
            m_r1 = argLocDesc.m_idxGenReg;
            m_r2 = m_r1 + argLocDesc.m_cGenReg - 1;
        }
        else if (argLocDesc.m_idxGenReg == m_r2 + 1 && !argIt.IsArgPassedByRef())
        {
            // Extend an existing range, but only if the argument is not passed by reference.
            // Arguments passed by reference are handled separately, because the interpreter stores the value types on its stack by value.
            m_r2 += argLocDesc.m_cGenReg;
        }
        else
        {
            // Discontinuous range - store a routine for the current and start a new one
            pRoutines[m_routineIndex++] = GetGPRegRangeLoadRoutine(m_r1, m_r2);
            m_r1 = argLocDesc.m_idxGenReg;
            m_r2 = m_r1 + argLocDesc.m_cGenReg - 1;
        }
    }

    if (argLocDesc.m_cFloatReg != 0)
    {
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
            pRoutines[m_routineIndex++] = GetFPRegRangeLoadRoutine(m_x1, m_x2);
            m_x1 = argLocDesc.m_idxFloatReg;
            m_x2 = m_x1 + argLocDesc.m_cFloatReg - 1;
        }
    }

    if (argLocDesc.m_byteStackSize != 0)
    {
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
            pRoutines[m_routineIndex++] = (PCODE)Load_Stack;
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
                    pRoutines[m_routineIndex++] = (PCODE)Load_Stack_1B;
                    break;
                case 2:
                    pRoutines[m_routineIndex++] = (PCODE)Load_Stack_2B;
                    break;
                case 4:
                    pRoutines[m_routineIndex++] = (PCODE)Load_Stack_4B;
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
    if (argIt.IsArgPassedByRef())
    {
        _ASSERTE(argLocDesc.m_cGenReg == 1);
        pRoutines[m_routineIndex++] = GetGPRegRefLoadRoutine(argLocDesc.m_idxGenReg);
        pRoutines[m_routineIndex++] = argIt.GetArgSize();
        m_r1 = NoRange;
    }
#endif // UNIX_AMD64_ABI
}

#endif // FEATURE_INTERPRETER
