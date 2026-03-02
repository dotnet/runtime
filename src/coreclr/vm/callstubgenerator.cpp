// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#if defined(FEATURE_INTERPRETER) && !defined(TARGET_WASM)

#include "common.h"
#include "callstubgenerator.h"
#include "callconvbuilder.hpp"
#include "ecall.h"
#include "dllimport.h"

extern "C" void InjectInterpStackAlign();
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

#if !defined(UNIX_AMD64_ABI) && defined(ENREGISTERED_PARAMTYPE_MAXSIZE)
extern "C" void Load_Stack_Ref();
extern "C" void Store_Stack_Ref();
#endif // !UNIX_AMD64_ABI && ENREGISTERED_PARAMTYPE_MAXSIZE

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

#if defined(TARGET_APPLE)
extern "C" void Load_SwiftSelf();
extern "C" void Load_SwiftSelf_ByRef();
extern "C" void Load_SwiftError();
extern "C" void Load_SwiftIndirectResult();

extern "C" void Load_X0_AtOffset();
extern "C" void Load_X1_AtOffset();
extern "C" void Load_X2_AtOffset();
extern "C" void Load_X3_AtOffset();
extern "C" void Load_X4_AtOffset();
extern "C" void Load_X5_AtOffset();
extern "C" void Load_X6_AtOffset();
extern "C" void Load_X7_AtOffset();
extern "C" void Load_D0_AtOffset();
extern "C" void Load_D1_AtOffset();
extern "C" void Load_D2_AtOffset();
extern "C" void Load_D3_AtOffset();
extern "C" void Load_D4_AtOffset();
extern "C" void Load_D5_AtOffset();
extern "C" void Load_D6_AtOffset();
extern "C" void Load_D7_AtOffset();
extern "C" void Load_Stack_AtOffset();

extern "C" void Store_X0_AtOffset();
extern "C" void Store_X1_AtOffset();
extern "C" void Store_X2_AtOffset();
extern "C" void Store_X3_AtOffset();
extern "C" void Store_D0_AtOffset();
extern "C" void Store_D1_AtOffset();
extern "C" void Store_D2_AtOffset();
extern "C" void Store_D3_AtOffset();
extern "C" void SwiftLoweredReturnTerminator();
#endif // TARGET_APPLE

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

// Q register function prototypes
extern "C" void Load_Q0();
extern "C" void Load_Q0_Q1();
extern "C" void Load_Q0_Q1_Q2();
extern "C" void Load_Q0_Q1_Q2_Q3();
extern "C" void Load_Q0_Q1_Q2_Q3_Q4();
extern "C" void Load_Q0_Q1_Q2_Q3_Q4_Q5();
extern "C" void Load_Q0_Q1_Q2_Q3_Q4_Q5_Q6();
extern "C" void Load_Q0_Q1_Q2_Q3_Q4_Q5_Q6_Q7();
extern "C" void Load_Q1();
extern "C" void Load_Q1_Q2();
extern "C" void Load_Q1_Q2_Q3();
extern "C" void Load_Q1_Q2_Q3_Q4();
extern "C" void Load_Q1_Q2_Q3_Q4_Q5();
extern "C" void Load_Q1_Q2_Q3_Q4_Q5_Q6();
extern "C" void Load_Q1_Q2_Q3_Q4_Q5_Q6_Q7();
extern "C" void Load_Q2();
extern "C" void Load_Q2_Q3();
extern "C" void Load_Q2_Q3_Q4();
extern "C" void Load_Q2_Q3_Q4_Q5();
extern "C" void Load_Q2_Q3_Q4_Q5_Q6();
extern "C" void Load_Q2_Q3_Q4_Q5_Q6_Q7();
extern "C" void Load_Q3();
extern "C" void Load_Q3_Q4();
extern "C" void Load_Q3_Q4_Q5();
extern "C" void Load_Q3_Q4_Q5_Q6();
extern "C" void Load_Q3_Q4_Q5_Q6_Q7();
extern "C" void Load_Q4();
extern "C" void Load_Q4_Q5();
extern "C" void Load_Q4_Q5_Q6();
extern "C" void Load_Q4_Q5_Q6_Q7();
extern "C" void Load_Q5();
extern "C" void Load_Q5_Q6();
extern "C" void Load_Q5_Q6_Q7();
extern "C" void Load_Q6();
extern "C" void Load_Q6_Q7();
extern "C" void Load_Q7();

extern "C" void Store_Q0();
extern "C" void Store_Q0_Q1();
extern "C" void Store_Q0_Q1_Q2();
extern "C" void Store_Q0_Q1_Q2_Q3();
extern "C" void Store_Q0_Q1_Q2_Q3_Q4();
extern "C" void Store_Q0_Q1_Q2_Q3_Q4_Q5();
extern "C" void Store_Q0_Q1_Q2_Q3_Q4_Q5_Q6();
extern "C" void Store_Q0_Q1_Q2_Q3_Q4_Q5_Q6_Q7();
extern "C" void Store_Q1();
extern "C" void Store_Q1_Q2();
extern "C" void Store_Q1_Q2_Q3();
extern "C" void Store_Q1_Q2_Q3_Q4();
extern "C" void Store_Q1_Q2_Q3_Q4_Q5();
extern "C" void Store_Q1_Q2_Q3_Q4_Q5_Q6();
extern "C" void Store_Q1_Q2_Q3_Q4_Q5_Q6_Q7();
extern "C" void Store_Q2();
extern "C" void Store_Q2_Q3();
extern "C" void Store_Q2_Q3_Q4();
extern "C" void Store_Q2_Q3_Q4_Q5();
extern "C" void Store_Q2_Q3_Q4_Q5_Q6();
extern "C" void Store_Q2_Q3_Q4_Q5_Q6_Q7();
extern "C" void Store_Q3();
extern "C" void Store_Q3_Q4();
extern "C" void Store_Q3_Q4_Q5();
extern "C" void Store_Q3_Q4_Q5_Q6();
extern "C" void Store_Q3_Q4_Q5_Q6_Q7();
extern "C" void Store_Q4();
extern "C" void Store_Q4_Q5();
extern "C" void Store_Q4_Q5_Q6();
extern "C" void Store_Q4_Q5_Q6_Q7();
extern "C" void Store_Q5();
extern "C" void Store_Q5_Q6();
extern "C" void Store_Q5_Q6_Q7();
extern "C" void Store_Q6();
extern "C" void Store_Q6_Q7();
extern "C" void Store_Q7();

// S register function prototypes
extern "C" void Load_S0();
extern "C" void Load_S0_S1();
extern "C" void Load_S0_S1_S2();
extern "C" void Load_S0_S1_S2_S3();
extern "C" void Load_S0_S1_S2_S3_S4();
extern "C" void Load_S0_S1_S2_S3_S4_S5();
extern "C" void Load_S0_S1_S2_S3_S4_S5_S6();
extern "C" void Load_S0_S1_S2_S3_S4_S5_S6_S7();
extern "C" void Load_S1();
extern "C" void Load_S1_S2();
extern "C" void Load_S1_S2_S3();
extern "C" void Load_S1_S2_S3_S4();
extern "C" void Load_S1_S2_S3_S4_S5();
extern "C" void Load_S1_S2_S3_S4_S5_S6();
extern "C" void Load_S1_S2_S3_S4_S5_S6_S7();
extern "C" void Load_S2();
extern "C" void Load_S2_S3();
extern "C" void Load_S2_S3_S4();
extern "C" void Load_S2_S3_S4_S5();
extern "C" void Load_S2_S3_S4_S5_S6();
extern "C" void Load_S2_S3_S4_S5_S6_S7();
extern "C" void Load_S3();
extern "C" void Load_S3_S4();
extern "C" void Load_S3_S4_S5();
extern "C" void Load_S3_S4_S5_S6();
extern "C" void Load_S3_S4_S5_S6_S7();
extern "C" void Load_S4();
extern "C" void Load_S4_S5();
extern "C" void Load_S4_S5_S6();
extern "C" void Load_S4_S5_S6_S7();
extern "C" void Load_S5();
extern "C" void Load_S5_S6();
extern "C" void Load_S5_S6_S7();
extern "C" void Load_S6();
extern "C" void Load_S6_S7();
extern "C" void Load_S7();

extern "C" void Store_S0();
extern "C" void Store_S0_S1();
extern "C" void Store_S0_S1_S2();
extern "C" void Store_S0_S1_S2_S3();
extern "C" void Store_S0_S1_S2_S3_S4();
extern "C" void Store_S0_S1_S2_S3_S4_S5();
extern "C" void Store_S0_S1_S2_S3_S4_S5_S6();
extern "C" void Store_S0_S1_S2_S3_S4_S5_S6_S7();
extern "C" void Store_S1();
extern "C" void Store_S1_S2();
extern "C" void Store_S1_S2_S3();
extern "C" void Store_S1_S2_S3_S4();
extern "C" void Store_S1_S2_S3_S4_S5();
extern "C" void Store_S1_S2_S3_S4_S5_S6();
extern "C" void Store_S1_S2_S3_S4_S5_S6_S7();
extern "C" void Store_S2();
extern "C" void Store_S2_S3();
extern "C" void Store_S2_S3_S4();
extern "C" void Store_S2_S3_S4_S5();
extern "C" void Store_S2_S3_S4_S5_S6();
extern "C" void Store_S2_S3_S4_S5_S6_S7();
extern "C" void Store_S3();
extern "C" void Store_S3_S4();
extern "C" void Store_S3_S4_S5();
extern "C" void Store_S3_S4_S5_S6();
extern "C" void Store_S3_S4_S5_S6_S7();
extern "C" void Store_S4();
extern "C" void Store_S4_S5();
extern "C" void Store_S4_S5_S6();
extern "C" void Store_S4_S5_S6_S7();
extern "C" void Store_S5();
extern "C" void Store_S5_S6();
extern "C" void Store_S5_S6_S7();
extern "C" void Store_S6();
extern "C" void Store_S6_S7();
extern "C" void Store_S7();

#endif // TARGET_ARM64

#ifdef TARGET_ARM

extern "C" void Load_R0();
extern "C" void Load_R0_R1();
extern "C" void Load_R0_R1_R2();
extern "C" void Load_R0_R1_R2_R3();
extern "C" void Load_R1();
extern "C" void Load_R1_R2();
extern "C" void Load_R1_R2_R3();
extern "C" void Load_R2();
extern "C" void Load_R2_R3();
extern "C" void Load_R3();

extern "C" void Store_R0();
extern "C" void Store_R0_R1();
extern "C" void Store_R0_R1_R2();
extern "C" void Store_R0_R1_R2_R3();
extern "C" void Store_R1();
extern "C" void Store_R1_R2();
extern "C" void Store_R1_R2_R3();
extern "C" void Store_R2();
extern "C" void Store_R2_R3();
extern "C" void Store_R3();

extern "C" void Load_R0_R1_4B();
extern "C" void Load_R0_R1_R2_R3_4B();
extern "C" void Load_R2_R3_4B();
extern "C" void Load_Stack_4B();
extern "C" void Store_R0_R1_4B();
extern "C" void Store_R0_R1_R2_R3_4B();
extern "C" void Store_R2_R3_4B();
extern "C" void Store_Stack_4B();

#endif // TARGET_ARM

#ifdef TARGET_RISCV64

extern "C" void Load_A0();
extern "C" void Load_A0_A1();
extern "C" void Load_A0_A1_A2();
extern "C" void Load_A0_A1_A2_A3();
extern "C" void Load_A0_A1_A2_A3_A4();
extern "C" void Load_A0_A1_A2_A3_A4_A5();
extern "C" void Load_A0_A1_A2_A3_A4_A5_A6();
extern "C" void Load_A0_A1_A2_A3_A4_A5_A6_A7();
extern "C" void Load_A1();
extern "C" void Load_A1_A2();
extern "C" void Load_A1_A2_A3();
extern "C" void Load_A1_A2_A3_A4();
extern "C" void Load_A1_A2_A3_A4_A5();
extern "C" void Load_A1_A2_A3_A4_A5_A6();
extern "C" void Load_A1_A2_A3_A4_A5_A6_A7();
extern "C" void Load_A2();
extern "C" void Load_A2_A3();
extern "C" void Load_A2_A3_A4();
extern "C" void Load_A2_A3_A4_A5();
extern "C" void Load_A2_A3_A4_A5_A6();
extern "C" void Load_A2_A3_A4_A5_A6_A7();
extern "C" void Load_A3();
extern "C" void Load_A3_A4();
extern "C" void Load_A3_A4_A5();
extern "C" void Load_A3_A4_A5_A6();
extern "C" void Load_A3_A4_A5_A6_A7();
extern "C" void Load_A4();
extern "C" void Load_A4_A5();
extern "C" void Load_A4_A5_A6();
extern "C" void Load_A4_A5_A6_A7();
extern "C" void Load_A5();
extern "C" void Load_A5_A6();
extern "C" void Load_A5_A6_A7();
extern "C" void Load_A6();
extern "C" void Load_A6_A7();
extern "C" void Load_A7();

extern "C" void Store_A0();
extern "C" void Store_A0_A1();
extern "C" void Store_A0_A1_A2();
extern "C" void Store_A0_A1_A2_A3();
extern "C" void Store_A0_A1_A2_A3_A4();
extern "C" void Store_A0_A1_A2_A3_A4_A5();
extern "C" void Store_A0_A1_A2_A3_A4_A5_A6();
extern "C" void Store_A0_A1_A2_A3_A4_A5_A6_A7();
extern "C" void Store_A1();
extern "C" void Store_A1_A2();
extern "C" void Store_A1_A2_A3();
extern "C" void Store_A1_A2_A3_A4();
extern "C" void Store_A1_A2_A3_A4_A5();
extern "C" void Store_A1_A2_A3_A4_A5_A6();
extern "C" void Store_A1_A2_A3_A4_A5_A6_A7();
extern "C" void Store_A2();
extern "C" void Store_A2_A3();
extern "C" void Store_A2_A3_A4();
extern "C" void Store_A2_A3_A4_A5();
extern "C" void Store_A2_A3_A4_A5_A6();
extern "C" void Store_A2_A3_A4_A5_A6_A7();
extern "C" void Store_A3();
extern "C" void Store_A3_A4();
extern "C" void Store_A3_A4_A5();
extern "C" void Store_A3_A4_A5_A6();
extern "C" void Store_A3_A4_A5_A6_A7();
extern "C" void Store_A4();
extern "C" void Store_A4_A5();
extern "C" void Store_A4_A5_A6();
extern "C" void Store_A4_A5_A6_A7();
extern "C" void Store_A5();
extern "C" void Store_A5_A6();
extern "C" void Store_A5_A6_A7();
extern "C" void Store_A6();
extern "C" void Store_A6_A7();
extern "C" void Store_A7();

extern "C" void Load_Ref_A0();
extern "C" void Load_Ref_A1();
extern "C" void Load_Ref_A2();
extern "C" void Load_Ref_A3();
extern "C" void Load_Ref_A4();
extern "C" void Load_Ref_A5();
extern "C" void Load_Ref_A6();
extern "C" void Load_Ref_A7();

extern "C" void Store_Ref_A0();
extern "C" void Store_Ref_A1();
extern "C" void Store_Ref_A2();
extern "C" void Store_Ref_A3();
extern "C" void Store_Ref_A4();
extern "C" void Store_Ref_A5();
extern "C" void Store_Ref_A6();
extern "C" void Store_Ref_A7();

extern "C" void Load_FA0();
extern "C" void Load_FA0_FA1();
extern "C" void Load_FA0_FA1_FA2();
extern "C" void Load_FA0_FA1_FA2_FA3();
extern "C" void Load_FA0_FA1_FA2_FA3_FA4();
extern "C" void Load_FA0_FA1_FA2_FA3_FA4_FA5();
extern "C" void Load_FA0_FA1_FA2_FA3_FA4_FA5_FA6();
extern "C" void Load_FA0_FA1_FA2_FA3_FA4_FA5_FA6_FA7();
extern "C" void Load_FA1();
extern "C" void Load_FA1_FA2();
extern "C" void Load_FA1_FA2_FA3();
extern "C" void Load_FA1_FA2_FA3_FA4();
extern "C" void Load_FA1_FA2_FA3_FA4_FA5();
extern "C" void Load_FA1_FA2_FA3_FA4_FA5_FA6();
extern "C" void Load_FA1_FA2_FA3_FA4_FA5_FA6_FA7();
extern "C" void Load_FA2();
extern "C" void Load_FA2_FA3();
extern "C" void Load_FA2_FA3_FA4();
extern "C" void Load_FA2_FA3_FA4_FA5();
extern "C" void Load_FA2_FA3_FA4_FA5_FA6();
extern "C" void Load_FA2_FA3_FA4_FA5_FA6_FA7();
extern "C" void Load_FA3();
extern "C" void Load_FA3_FA4();
extern "C" void Load_FA3_FA4_FA5();
extern "C" void Load_FA3_FA4_FA5_FA6();
extern "C" void Load_FA3_FA4_FA5_FA6_FA7();
extern "C" void Load_FA4();
extern "C" void Load_FA4_FA5();
extern "C" void Load_FA4_FA5_FA6();
extern "C" void Load_FA4_FA5_FA6_FA7();
extern "C" void Load_FA5();
extern "C" void Load_FA5_FA6();
extern "C" void Load_FA5_FA6_FA7();
extern "C" void Load_FA6();
extern "C" void Load_FA6_FA7();
extern "C" void Load_FA7();

extern "C" void Store_FA0();
extern "C" void Store_FA0_FA1();
extern "C" void Store_FA0_FA1_FA2();
extern "C" void Store_FA0_FA1_FA2_FA3();
extern "C" void Store_FA0_FA1_FA2_FA3_FA4();
extern "C" void Store_FA0_FA1_FA2_FA3_FA4_FA5();
extern "C" void Store_FA0_FA1_FA2_FA3_FA4_FA5_FA6();
extern "C" void Store_FA0_FA1_FA2_FA3_FA4_FA5_FA6_FA7();
extern "C" void Store_FA1();
extern "C" void Store_FA1_FA2();
extern "C" void Store_FA1_FA2_FA3();
extern "C" void Store_FA1_FA2_FA3_FA4();
extern "C" void Store_FA1_FA2_FA3_FA4_FA5();
extern "C" void Store_FA1_FA2_FA3_FA4_FA5_FA6();
extern "C" void Store_FA1_FA2_FA3_FA4_FA5_FA6_FA7();
extern "C" void Store_FA2();
extern "C" void Store_FA2_FA3();
extern "C" void Store_FA2_FA3_FA4();
extern "C" void Store_FA2_FA3_FA4_FA5();
extern "C" void Store_FA2_FA3_FA4_FA5_FA6();
extern "C" void Store_FA2_FA3_FA4_FA5_FA6_FA7();
extern "C" void Store_FA3();
extern "C" void Store_FA3_FA4();
extern "C" void Store_FA3_FA4_FA5();
extern "C" void Store_FA3_FA4_FA5_FA6();
extern "C" void Store_FA3_FA4_FA5_FA6_FA7();
extern "C" void Store_FA4();
extern "C" void Store_FA4_FA5();
extern "C" void Store_FA4_FA5_FA6();
extern "C" void Store_FA4_FA5_FA6_FA7();
extern "C" void Store_FA5();
extern "C" void Store_FA5_FA6();
extern "C" void Store_FA5_FA6_FA7();
extern "C" void Store_FA6();
extern "C" void Store_FA6_FA7();
extern "C" void Store_FA7();

#endif // TARGET_RISCV64

PCODE CallStubGenerator::GetStackRoutine()
{
    LOG2((LF2_INTERPRETER, LL_INFO10000, "Load_Stack\n"));
    return m_interpreterToNative ? (PCODE)Load_Stack : (PCODE)Store_Stack;
}

#if defined(TARGET_APPLE) && defined(TARGET_ARM64)
PCODE CallStubGenerator::GetStackRoutine_1B()
{
    LOG2((LF2_INTERPRETER, LL_INFO10000, "GetStackRoutine_1B\n"));
    return m_interpreterToNative ? (PCODE)Load_Stack_1B : (PCODE)Store_Stack_1B;
}

PCODE CallStubGenerator::GetStackRoutine_2B()
{
    LOG2((LF2_INTERPRETER, LL_INFO10000, "GetStackRoutine_2B\n"));
    return m_interpreterToNative ? (PCODE)Load_Stack_2B : (PCODE)Store_Stack_2B;
}

PCODE CallStubGenerator::GetStackRoutine_4B()
{
    LOG2((LF2_INTERPRETER, LL_INFO10000, "GetStackRoutine_4B\n"));
    return m_interpreterToNative ? (PCODE)Load_Stack_4B : (PCODE)Store_Stack_4B;
}
#endif // TARGET_APPLE && TARGET_ARM64

PCODE CallStubGenerator::GetGPRegRangeRoutine(int r1, int r2)
{
    LOG2((LF2_INTERPRETER, LL_INFO10000, "GetGPRegRangeRoutine %d %d\n", r1, r2));

#if defined(TARGET_AMD64) && defined(TARGET_WINDOWS)
    static const PCODE GPRegsLoadRoutines[] = {
        (PCODE)Load_RCX, (PCODE)Load_RCX_RDX, (PCODE)Load_RCX_RDX_R8, (PCODE)Load_RCX_RDX_R8_R9,
        (PCODE)0, (PCODE)Load_RDX, (PCODE)Load_RDX_R8, (PCODE)Load_RDX_R8_R9,
        (PCODE)0, (PCODE)0, (PCODE)Load_R8, (PCODE)Load_R8_R9,
        (PCODE)0, (PCODE)0, (PCODE)0, (PCODE)Load_R9
    };
    static const PCODE GPRegsStoreRoutines[] = {
        (PCODE)Store_RCX, (PCODE)Store_RCX_RDX, (PCODE)Store_RCX_RDX_R8, (PCODE)Store_RCX_RDX_R8_R9,
        (PCODE)0, (PCODE)Store_RDX, (PCODE)Store_RDX_R8, (PCODE)Store_RDX_R8_R9,
        (PCODE)0, (PCODE)0, (PCODE)Store_R8, (PCODE)Store_R8_R9,
        (PCODE)0, (PCODE)0, (PCODE)0, (PCODE)Store_R9
    };
#elif defined(TARGET_AMD64) // Unix AMD64
    static const PCODE GPRegsLoadRoutines[] = {
        (PCODE)Load_RDI, (PCODE)Load_RDI_RSI, (PCODE)Load_RDI_RSI_RDX, (PCODE)Load_RDI_RSI_RDX_RCX, (PCODE)Load_RDI_RSI_RDX_RCX_R8, (PCODE)Load_RDI_RSI_RDX_RCX_R8_R9,
        (PCODE)0, (PCODE)Load_RSI, (PCODE)Load_RSI_RDX, (PCODE)Load_RSI_RDX_RCX, (PCODE)Load_RSI_RDX_RCX_R8, (PCODE)Load_RSI_RDX_RCX_R8_R9,
        (PCODE)0, (PCODE)0, (PCODE)Load_RDX, (PCODE)Load_RDX_RCX, (PCODE)Load_RDX_RCX_R8, (PCODE)Load_RDX_RCX_R8_R9,
        (PCODE)0, (PCODE)0, (PCODE)0, (PCODE)Load_RCX, (PCODE)Load_RCX_R8, (PCODE)Load_RCX_R8_R9,
        (PCODE)0, (PCODE)0, (PCODE)0, (PCODE)0, (PCODE)Load_R8, (PCODE)Load_R8_R9,
        (PCODE)0, (PCODE)0, (PCODE)0, (PCODE)0, (PCODE)0, (PCODE)Load_R9
    };
    static const PCODE GPRegsStoreRoutines[] = {
        (PCODE)Store_RDI, (PCODE)Store_RDI_RSI, (PCODE)Store_RDI_RSI_RDX, (PCODE)Store_RDI_RSI_RDX_RCX, (PCODE)Store_RDI_RSI_RDX_RCX_R8, (PCODE)Store_RDI_RSI_RDX_RCX_R8_R9,
        (PCODE)0, (PCODE)Store_RSI, (PCODE)Store_RSI_RDX, (PCODE)Store_RSI_RDX_RCX, (PCODE)Store_RSI_RDX_RCX_R8, (PCODE)Store_RSI_RDX_RCX_R8_R9,
        (PCODE)0, (PCODE)0, (PCODE)Store_RDX, (PCODE)Store_RDX_RCX, (PCODE)Store_RDX_RCX_R8, (PCODE)Store_RDX_RCX_R8_R9,
        (PCODE)0, (PCODE)0, (PCODE)0, (PCODE)Store_RCX, (PCODE)Store_RCX_R8, (PCODE)Store_RCX_R8_R9,
        (PCODE)0, (PCODE)0, (PCODE)0, (PCODE)0, (PCODE)Store_R8, (PCODE)Store_R8_R9,
        (PCODE)0, (PCODE)0, (PCODE)0, (PCODE)0, (PCODE)0, (PCODE)Store_R9
    };
#elif defined(TARGET_ARM64)
    static const PCODE GPRegsLoadRoutines[] = {
        (PCODE)Load_X0, (PCODE)Load_X0_X1, (PCODE)Load_X0_X1_X2, (PCODE)Load_X0_X1_X2_X3, (PCODE)Load_X0_X1_X2_X3_X4, (PCODE)Load_X0_X1_X2_X3_X4_X5, (PCODE)Load_X0_X1_X2_X3_X4_X5_X6, (PCODE)Load_X0_X1_X2_X3_X4_X5_X6_X7,
        (PCODE)0, (PCODE)Load_X1, (PCODE)Load_X1_X2, (PCODE)Load_X1_X2_X3, (PCODE)Load_X1_X2_X3_X4, (PCODE)Load_X1_X2_X3_X4_X5, (PCODE)Load_X1_X2_X3_X4_X5_X6, (PCODE)Load_X1_X2_X3_X4_X5_X6_X7,
        (PCODE)0, (PCODE)0, (PCODE)Load_X2, (PCODE)Load_X2_X3, (PCODE)Load_X2_X3_X4, (PCODE)Load_X2_X3_X4_X5, (PCODE)Load_X2_X3_X4_X5_X6, (PCODE)Load_X2_X3_X4_X5_X6_X7,
        (PCODE)0, (PCODE)0, (PCODE)0, (PCODE)Load_X3, (PCODE)Load_X3_X4, (PCODE)Load_X3_X4_X5, (PCODE)Load_X3_X4_X5_X6, (PCODE)Load_X3_X4_X5_X6_X7,
        (PCODE)0, (PCODE)0, (PCODE)0, (PCODE)0, (PCODE)Load_X4, (PCODE)Load_X4_X5, (PCODE)Load_X4_X5_X6, (PCODE)Load_X4_X5_X6_X7,
        (PCODE)0, (PCODE)0, (PCODE)0, (PCODE)0, (PCODE)0, (PCODE)Load_X5, (PCODE)Load_X5_X6, (PCODE)Load_X5_X6_X7,
        (PCODE)0, (PCODE)0, (PCODE)0, (PCODE)0, (PCODE)0, (PCODE)0, (PCODE)Load_X6, (PCODE)Load_X6_X7,
        (PCODE)0, (PCODE)0, (PCODE)0, (PCODE)0, (PCODE)0, (PCODE)0, (PCODE)0, (PCODE)Load_X7
    };
    static const PCODE GPRegsStoreRoutines[] = {
        (PCODE)Store_X0, (PCODE)Store_X0_X1, (PCODE)Store_X0_X1_X2, (PCODE)Store_X0_X1_X2_X3, (PCODE)Store_X0_X1_X2_X3_X4, (PCODE)Store_X0_X1_X2_X3_X4_X5, (PCODE)Store_X0_X1_X2_X3_X4_X5_X6, (PCODE)Store_X0_X1_X2_X3_X4_X5_X6_X7,
        (PCODE)0, (PCODE)Store_X1, (PCODE)Store_X1_X2, (PCODE)Store_X1_X2_X3, (PCODE)Store_X1_X2_X3_X4, (PCODE)Store_X1_X2_X3_X4_X5, (PCODE)Store_X1_X2_X3_X4_X5_X6, (PCODE)Store_X1_X2_X3_X4_X5_X6_X7,
        (PCODE)0, (PCODE)0, (PCODE)Store_X2, (PCODE)Store_X2_X3, (PCODE)Store_X2_X3_X4, (PCODE)Store_X2_X3_X4_X5, (PCODE)Store_X2_X3_X4_X5_X6, (PCODE)Store_X2_X3_X4_X5_X6_X7,
        (PCODE)0, (PCODE)0, (PCODE)0, (PCODE)Store_X3, (PCODE)Store_X3_X4, (PCODE)Store_X3_X4_X5, (PCODE)Store_X3_X4_X5_X6, (PCODE)Store_X3_X4_X5_X6_X7,
        (PCODE)0, (PCODE)0, (PCODE)0, (PCODE)0, (PCODE)Store_X4, (PCODE)Store_X4_X5, (PCODE)Store_X4_X5_X6, (PCODE)Store_X4_X5_X6_X7,
        (PCODE)0, (PCODE)0, (PCODE)0, (PCODE)0, (PCODE)0, (PCODE)Store_X5, (PCODE)Store_X5_X6, (PCODE)Store_X5_X6_X7,
        (PCODE)0, (PCODE)0, (PCODE)0, (PCODE)0, (PCODE)0, (PCODE)0, (PCODE)Store_X6, (PCODE)Store_X6_X7,
        (PCODE)0, (PCODE)0, (PCODE)0, (PCODE)0, (PCODE)0, (PCODE)0, (PCODE)0, (PCODE)Store_X7
    };
#elif defined(TARGET_ARM)
    static const PCODE GPRegsLoadRoutines[] = {
        (PCODE)Load_R0, (PCODE)Load_R0_R1, (PCODE)Load_R0_R1_R2, (PCODE)Load_R0_R1_R2_R3,
        (PCODE)0, (PCODE)Load_R1, (PCODE)Load_R1_R2, (PCODE)Load_R1_R2_R3,
        (PCODE)0, (PCODE)0, (PCODE)Load_R2, (PCODE)Load_R2_R3,
        (PCODE)0, (PCODE)0, (PCODE)0, (PCODE)Load_R3
    };
    static const PCODE GPRegsStoreRoutines[] = {
        (PCODE)Store_R0, (PCODE)Store_R0_R1, (PCODE)Store_R0_R1_R2, (PCODE)Store_R0_R1_R2_R3,
        (PCODE)0, (PCODE)Store_R1, (PCODE)Store_R1_R2, (PCODE)Store_R1_R2_R3,
        (PCODE)0, (PCODE)0, (PCODE)Store_R2, (PCODE)Store_R2_R3,
        (PCODE)0, (PCODE)0, (PCODE)0, (PCODE)Store_R3
    };
#elif defined(TARGET_RISCV64)
    static const PCODE GPRegsLoadRoutines[] = {
        (PCODE)Load_A0, (PCODE)Load_A0_A1, (PCODE)Load_A0_A1_A2, (PCODE)Load_A0_A1_A2_A3, (PCODE)Load_A0_A1_A2_A3_A4, (PCODE)Load_A0_A1_A2_A3_A4_A5, (PCODE)Load_A0_A1_A2_A3_A4_A5_A6, (PCODE)Load_A0_A1_A2_A3_A4_A5_A6_A7,
        (PCODE)0, (PCODE)Load_A1, (PCODE)Load_A1_A2, (PCODE)Load_A1_A2_A3, (PCODE)Load_A1_A2_A3_A4, (PCODE)Load_A1_A2_A3_A4_A5, (PCODE)Load_A1_A2_A3_A4_A5_A6, (PCODE)Load_A1_A2_A3_A4_A5_A6_A7,
        (PCODE)0, (PCODE)0, (PCODE)Load_A2, (PCODE)Load_A2_A3, (PCODE)Load_A2_A3_A4, (PCODE)Load_A2_A3_A4_A5, (PCODE)Load_A2_A3_A4_A5_A6, (PCODE)Load_A2_A3_A4_A5_A6_A7,
        (PCODE)0, (PCODE)0, (PCODE)0, (PCODE)Load_A3, (PCODE)Load_A3_A4, (PCODE)Load_A3_A4_A5, (PCODE)Load_A3_A4_A5_A6, (PCODE)Load_A3_A4_A5_A6_A7,
        (PCODE)0, (PCODE)0, (PCODE)0, (PCODE)0, (PCODE)Load_A4, (PCODE)Load_A4_A5, (PCODE)Load_A4_A5_A6, (PCODE)Load_A4_A5_A6_A7,
        (PCODE)0, (PCODE)0, (PCODE)0, (PCODE)0, (PCODE)0, (PCODE)Load_A5, (PCODE)Load_A5_A6, (PCODE)Load_A5_A6_A7,
        (PCODE)0, (PCODE)0, (PCODE)0, (PCODE)0, (PCODE)0, (PCODE)0, (PCODE)Load_A6, (PCODE)Load_A6_A7,
        (PCODE)0, (PCODE)0, (PCODE)0, (PCODE)0, (PCODE)0, (PCODE)0, (PCODE)0, (PCODE)Load_A7
    };
    static const PCODE GPRegsStoreRoutines[] = {
        (PCODE)Store_A0, (PCODE)Store_A0_A1, (PCODE)Store_A0_A1_A2, (PCODE)Store_A0_A1_A2_A3, (PCODE)Store_A0_A1_A2_A3_A4, (PCODE)Store_A0_A1_A2_A3_A4_A5, (PCODE)Store_A0_A1_A2_A3_A4_A5_A6, (PCODE)Store_A0_A1_A2_A3_A4_A5_A6_A7,
        (PCODE)0, (PCODE)Store_A1, (PCODE)Store_A1_A2, (PCODE)Store_A1_A2_A3, (PCODE)Store_A1_A2_A3_A4, (PCODE)Store_A1_A2_A3_A4_A5, (PCODE)Store_A1_A2_A3_A4_A5_A6, (PCODE)Store_A1_A2_A3_A4_A5_A6_A7,
        (PCODE)0, (PCODE)0, (PCODE)Store_A2, (PCODE)Store_A2_A3, (PCODE)Store_A2_A3_A4, (PCODE)Store_A2_A3_A4_A5, (PCODE)Store_A2_A3_A4_A5_A6, (PCODE)Store_A2_A3_A4_A5_A6_A7,
        (PCODE)0, (PCODE)0, (PCODE)0, (PCODE)Store_A3, (PCODE)Store_A3_A4, (PCODE)Store_A3_A4_A5, (PCODE)Store_A3_A4_A5_A6, (PCODE)Store_A3_A4_A5_A6_A7,
        (PCODE)0, (PCODE)0, (PCODE)0, (PCODE)0, (PCODE)Store_A4, (PCODE)Store_A4_A5, (PCODE)Store_A4_A5_A6, (PCODE)Store_A4_A5_A6_A7,
        (PCODE)0, (PCODE)0, (PCODE)0, (PCODE)0, (PCODE)0, (PCODE)Store_A5, (PCODE)Store_A5_A6, (PCODE)Store_A5_A6_A7,
        (PCODE)0, (PCODE)0, (PCODE)0, (PCODE)0, (PCODE)0, (PCODE)0, (PCODE)Store_A6, (PCODE)Store_A6_A7,
        (PCODE)0, (PCODE)0, (PCODE)0, (PCODE)0, (PCODE)0, (PCODE)0, (PCODE)0, (PCODE)Store_A7
    };
#endif

    int index = r1 * NUM_ARGUMENT_REGISTERS + r2;
    PCODE routine = m_interpreterToNative ? GPRegsLoadRoutines[index] : GPRegsStoreRoutines[index];
    _ASSERTE(routine != 0);
    return routine;
}

#if !defined(UNIX_AMD64_ABI) && defined(ENREGISTERED_PARAMTYPE_MAXSIZE)
PCODE CallStubGenerator::GetGPRegRefRoutine(int r)
{
#if LOG_COMPUTE_CALL_STUB
    LOG2((LF2_INTERPRETER, LL_INFO10000, "GetGPRegRefRoutine %d\n", r));
#endif

#if defined(TARGET_AMD64) && defined(TARGET_WINDOWS)
    static const PCODE GPRegsRefLoadRoutines[] = {
        (PCODE)Load_Ref_RCX, (PCODE)Load_Ref_RDX, (PCODE)Load_Ref_R8, (PCODE)Load_Ref_R9
    };
    static const PCODE GPRegsRefStoreRoutines[] = {
        (PCODE)Store_Ref_RCX, (PCODE)Store_Ref_RDX, (PCODE)Store_Ref_R8, (PCODE)Store_Ref_R9
    };
#elif defined(TARGET_ARM64)
    static const PCODE GPRegsRefLoadRoutines[] = {
        (PCODE)Load_Ref_X0, (PCODE)Load_Ref_X1, (PCODE)Load_Ref_X2, (PCODE)Load_Ref_X3,
        (PCODE)Load_Ref_X4, (PCODE)Load_Ref_X5, (PCODE)Load_Ref_X6, (PCODE)Load_Ref_X7
    };
    static const PCODE GPRegsRefStoreRoutines[] = {
        (PCODE)Store_Ref_X0, (PCODE)Store_Ref_X1, (PCODE)Store_Ref_X2, (PCODE)Store_Ref_X3,
        (PCODE)Store_Ref_X4, (PCODE)Store_Ref_X5, (PCODE)Store_Ref_X6, (PCODE)Store_Ref_X7
    };
#elif defined(TARGET_RISCV64)
    static const PCODE GPRegsRefLoadRoutines[] = {
        (PCODE)Load_Ref_A0, (PCODE)Load_Ref_A1, (PCODE)Load_Ref_A2, (PCODE)Load_Ref_A3,
        (PCODE)Load_Ref_A4, (PCODE)Load_Ref_A5, (PCODE)Load_Ref_A6, (PCODE)Load_Ref_A7
    };
    static const PCODE GPRegsRefStoreRoutines[] = {
        (PCODE)Store_Ref_A0, (PCODE)Store_Ref_A1, (PCODE)Store_Ref_A2, (PCODE)Store_Ref_A3,
        (PCODE)Store_Ref_A4, (PCODE)Store_Ref_A5, (PCODE)Store_Ref_A6, (PCODE)Store_Ref_A7
    };
#endif

    return m_interpreterToNative ? GPRegsRefLoadRoutines[r] : GPRegsRefStoreRoutines[r];
}

PCODE CallStubGenerator::GetStackRefRoutine()
{
    LOG2((LF2_INTERPRETER, LL_INFO10000, "GetStackRefRoutine\n"));
    return m_interpreterToNative ? (PCODE)Load_Stack_Ref : (PCODE)Store_Stack_Ref;
}

#endif // !UNIX_AMD64_ABI && ENREGISTERED_PARAMTYPE_MAXSIZE

PCODE CallStubGenerator::GetFPRegRangeRoutine(int x1, int x2)
{
    LOG2((LF2_INTERPRETER, LL_INFO10000, "GetFPRegRangeRoutine %d %d\n", x1, x2));

#ifdef TARGET_ARM
    _ASSERTE(!"Not support FP reg yet");
    return 0;
#else

#if defined(TARGET_AMD64) && defined(TARGET_WINDOWS)
    static const PCODE FPRegsLoadRoutines[] = {
        (PCODE)Load_XMM0, (PCODE)Load_XMM0_XMM1, (PCODE)Load_XMM0_XMM1_XMM2, (PCODE)Load_XMM0_XMM1_XMM2_XMM3,
        (PCODE)0, (PCODE)Load_XMM1, (PCODE)Load_XMM1_XMM2, (PCODE)Load_XMM1_XMM2_XMM3,
        (PCODE)0, (PCODE)0, (PCODE)Load_XMM2, (PCODE)Load_XMM2_XMM3,
        (PCODE)0, (PCODE)0, (PCODE)0, (PCODE)Load_XMM3
    };
    static const PCODE FPRegsStoreRoutines[] = {
        (PCODE)Store_XMM0, (PCODE)Store_XMM0_XMM1, (PCODE)Store_XMM0_XMM1_XMM2, (PCODE)Store_XMM0_XMM1_XMM2_XMM3,
        (PCODE)0, (PCODE)Store_XMM1, (PCODE)Store_XMM1_XMM2, (PCODE)Store_XMM1_XMM2_XMM3,
        (PCODE)0, (PCODE)0, (PCODE)Store_XMM2, (PCODE)Store_XMM2_XMM3,
        (PCODE)0, (PCODE)0, (PCODE)0, (PCODE)Store_XMM3
    };
#elif defined(TARGET_AMD64) // Unix AMD64
    static const PCODE FPRegsLoadRoutines[] = {
        (PCODE)Load_XMM0, (PCODE)Load_XMM0_XMM1, (PCODE)Load_XMM0_XMM1_XMM2, (PCODE)Load_XMM0_XMM1_XMM2_XMM3, (PCODE)Load_XMM0_XMM1_XMM2_XMM3_XMM4, (PCODE)Load_XMM0_XMM1_XMM2_XMM3_XMM4_XMM5, (PCODE)Load_XMM0_XMM1_XMM2_XMM3_XMM4_XMM5_XMM6, (PCODE)Load_XMM0_XMM1_XMM2_XMM3_XMM4_XMM5_XMM6_XMM7,
        (PCODE)0, (PCODE)Load_XMM1, (PCODE)Load_XMM1_XMM2, (PCODE)Load_XMM1_XMM2_XMM3, (PCODE)Load_XMM1_XMM2_XMM3_XMM4, (PCODE)Load_XMM1_XMM2_XMM3_XMM4_XMM5, (PCODE)Load_XMM1_XMM2_XMM3_XMM4_XMM5_XMM6, (PCODE)Load_XMM1_XMM2_XMM3_XMM4_XMM5_XMM6_XMM7,
        (PCODE)0, (PCODE)0, (PCODE)Load_XMM2, (PCODE)Load_XMM2_XMM3, (PCODE)Load_XMM2_XMM3_XMM4, (PCODE)Load_XMM2_XMM3_XMM4_XMM5, (PCODE)Load_XMM2_XMM3_XMM4_XMM5_XMM6, (PCODE)Load_XMM2_XMM3_XMM4_XMM5_XMM6_XMM7,
        (PCODE)0, (PCODE)0, (PCODE)0, (PCODE)Load_XMM3, (PCODE)Load_XMM3_XMM4, (PCODE)Load_XMM3_XMM4_XMM5, (PCODE)Load_XMM3_XMM4_XMM5_XMM6, (PCODE)Load_XMM3_XMM4_XMM5_XMM6_XMM7,
        (PCODE)0, (PCODE)0, (PCODE)0, (PCODE)0, (PCODE)Load_XMM4, (PCODE)Load_XMM4_XMM5, (PCODE)Load_XMM4_XMM5_XMM6, (PCODE)Load_XMM4_XMM5_XMM6_XMM7,
        (PCODE)0, (PCODE)0, (PCODE)0, (PCODE)0, (PCODE)0, (PCODE)Load_XMM5, (PCODE)Load_XMM5_XMM6, (PCODE)Load_XMM5_XMM6_XMM7,
        (PCODE)0, (PCODE)0, (PCODE)0, (PCODE)0, (PCODE)0, (PCODE)0, (PCODE)Load_XMM6, (PCODE)Load_XMM6_XMM7,
        (PCODE)0, (PCODE)0, (PCODE)0, (PCODE)0, (PCODE)0, (PCODE)0, (PCODE)0, (PCODE)Load_XMM7
    };
    static const PCODE FPRegsStoreRoutines[] = {
        (PCODE)Store_XMM0, (PCODE)Store_XMM0_XMM1, (PCODE)Store_XMM0_XMM1_XMM2, (PCODE)Store_XMM0_XMM1_XMM2_XMM3, (PCODE)Store_XMM0_XMM1_XMM2_XMM3_XMM4, (PCODE)Store_XMM0_XMM1_XMM2_XMM3_XMM4_XMM5, (PCODE)Store_XMM0_XMM1_XMM2_XMM3_XMM4_XMM5_XMM6, (PCODE)Store_XMM0_XMM1_XMM2_XMM3_XMM4_XMM5_XMM6_XMM7,
        (PCODE)0, (PCODE)Store_XMM1, (PCODE)Store_XMM1_XMM2, (PCODE)Store_XMM1_XMM2_XMM3, (PCODE)Store_XMM1_XMM2_XMM3_XMM4, (PCODE)Store_XMM1_XMM2_XMM3_XMM4_XMM5, (PCODE)Store_XMM1_XMM2_XMM3_XMM4_XMM5_XMM6, (PCODE)Store_XMM1_XMM2_XMM3_XMM4_XMM5_XMM6_XMM7,
        (PCODE)0, (PCODE)0, (PCODE)Store_XMM2, (PCODE)Store_XMM2_XMM3, (PCODE)Store_XMM2_XMM3_XMM4, (PCODE)Store_XMM2_XMM3_XMM4_XMM5, (PCODE)Store_XMM2_XMM3_XMM4_XMM5_XMM6, (PCODE)Store_XMM2_XMM3_XMM4_XMM5_XMM6_XMM7,
        (PCODE)0, (PCODE)0, (PCODE)0, (PCODE)Store_XMM3, (PCODE)Store_XMM3_XMM4, (PCODE)Store_XMM3_XMM4_XMM5, (PCODE)Store_XMM3_XMM4_XMM5_XMM6, (PCODE)Store_XMM3_XMM4_XMM5_XMM6_XMM7,
        (PCODE)0, (PCODE)0, (PCODE)0, (PCODE)0, (PCODE)Store_XMM4, (PCODE)Store_XMM4_XMM5, (PCODE)Store_XMM4_XMM5_XMM6, (PCODE)Store_XMM4_XMM5_XMM6_XMM7,
        (PCODE)0, (PCODE)0, (PCODE)0, (PCODE)0, (PCODE)0, (PCODE)Store_XMM5, (PCODE)Store_XMM5_XMM6, (PCODE)Store_XMM5_XMM6_XMM7,
        (PCODE)0, (PCODE)0, (PCODE)0, (PCODE)0, (PCODE)0, (PCODE)0, (PCODE)Store_XMM6, (PCODE)Store_XMM6_XMM7,
        (PCODE)0, (PCODE)0, (PCODE)0, (PCODE)0, (PCODE)0, (PCODE)0, (PCODE)0, (PCODE)Store_XMM7
    };
#elif defined(TARGET_ARM64)
    static const PCODE FPRegsLoadRoutines[] = {
        (PCODE)Load_D0, (PCODE)Load_D0_D1, (PCODE)Load_D0_D1_D2, (PCODE)Load_D0_D1_D2_D3, (PCODE)Load_D0_D1_D2_D3_D4, (PCODE)Load_D0_D1_D2_D3_D4_D5, (PCODE)Load_D0_D1_D2_D3_D4_D5_D6, (PCODE)Load_D0_D1_D2_D3_D4_D5_D6_D7,
        (PCODE)0, (PCODE)Load_D1, (PCODE)Load_D1_D2, (PCODE)Load_D1_D2_D3, (PCODE)Load_D1_D2_D3_D4, (PCODE)Load_D1_D2_D3_D4_D5, (PCODE)Load_D1_D2_D3_D4_D5_D6, (PCODE)Load_D1_D2_D3_D4_D5_D6_D7,
        (PCODE)0, (PCODE)0, (PCODE)Load_D2, (PCODE)Load_D2_D3, (PCODE)Load_D2_D3_D4, (PCODE)Load_D2_D3_D4_D5, (PCODE)Load_D2_D3_D4_D5_D6, (PCODE)Load_D2_D3_D4_D5_D6_D7,
        (PCODE)0, (PCODE)0, (PCODE)0, (PCODE)Load_D3, (PCODE)Load_D3_D4, (PCODE)Load_D3_D4_D5, (PCODE)Load_D3_D4_D5_D6, (PCODE)Load_D3_D4_D5_D6_D7,
        (PCODE)0, (PCODE)0, (PCODE)0, (PCODE)0, (PCODE)Load_D4, (PCODE)Load_D4_D5, (PCODE)Load_D4_D5_D6, (PCODE)Load_D4_D5_D6_D7,
        (PCODE)0, (PCODE)0, (PCODE)0, (PCODE)0, (PCODE)0, (PCODE)Load_D5, (PCODE)Load_D5_D6, (PCODE)Load_D5_D6_D7,
        (PCODE)0, (PCODE)0, (PCODE)0, (PCODE)0, (PCODE)0, (PCODE)0, (PCODE)Load_D6, (PCODE)Load_D6_D7,
        (PCODE)0, (PCODE)0, (PCODE)0, (PCODE)0, (PCODE)0, (PCODE)0, (PCODE)0, (PCODE)Load_D7
    };
    static const PCODE FPRegsStoreRoutines[] = {
        (PCODE)Store_D0, (PCODE)Store_D0_D1, (PCODE)Store_D0_D1_D2, (PCODE)Store_D0_D1_D2_D3, (PCODE)Store_D0_D1_D2_D3_D4, (PCODE)Store_D0_D1_D2_D3_D4_D5, (PCODE)Store_D0_D1_D2_D3_D4_D5_D6, (PCODE)Store_D0_D1_D2_D3_D4_D5_D6_D7,
        (PCODE)0, (PCODE)Store_D1, (PCODE)Store_D1_D2, (PCODE)Store_D1_D2_D3, (PCODE)Store_D1_D2_D3_D4, (PCODE)Store_D1_D2_D3_D4_D5, (PCODE)Store_D1_D2_D3_D4_D5_D6, (PCODE)Store_D1_D2_D3_D4_D5_D6_D7,
        (PCODE)0, (PCODE)0, (PCODE)Store_D2, (PCODE)Store_D2_D3, (PCODE)Store_D2_D3_D4, (PCODE)Store_D2_D3_D4_D5, (PCODE)Store_D2_D3_D4_D5_D6, (PCODE)Store_D2_D3_D4_D5_D6_D7,
        (PCODE)0, (PCODE)0, (PCODE)0, (PCODE)Store_D3, (PCODE)Store_D3_D4, (PCODE)Store_D3_D4_D5, (PCODE)Store_D3_D4_D5_D6, (PCODE)Store_D3_D4_D5_D6_D7,
        (PCODE)0, (PCODE)0, (PCODE)0, (PCODE)0, (PCODE)Store_D4, (PCODE)Store_D4_D5, (PCODE)Store_D4_D5_D6, (PCODE)Store_D4_D5_D6_D7,
        (PCODE)0, (PCODE)0, (PCODE)0, (PCODE)0, (PCODE)0, (PCODE)Store_D5, (PCODE)Store_D5_D6, (PCODE)Store_D5_D6_D7,
        (PCODE)0, (PCODE)0, (PCODE)0, (PCODE)0, (PCODE)0, (PCODE)0, (PCODE)Store_D6, (PCODE)Store_D6_D7,
        (PCODE)0, (PCODE)0, (PCODE)0, (PCODE)0, (PCODE)0, (PCODE)0, (PCODE)0, (PCODE)Store_D7
    };
#elif defined(TARGET_RISCV64)
    static const PCODE FPRegsLoadRoutines[] = {
        (PCODE)Load_FA0, (PCODE)Load_FA0_FA1, (PCODE)Load_FA0_FA1_FA2, (PCODE)Load_FA0_FA1_FA2_FA3, (PCODE)Load_FA0_FA1_FA2_FA3_FA4, (PCODE)Load_FA0_FA1_FA2_FA3_FA4_FA5, (PCODE)Load_FA0_FA1_FA2_FA3_FA4_FA5_FA6, (PCODE)Load_FA0_FA1_FA2_FA3_FA4_FA5_FA6_FA7,
        (PCODE)0, (PCODE)Load_FA1, (PCODE)Load_FA1_FA2, (PCODE)Load_FA1_FA2_FA3, (PCODE)Load_FA1_FA2_FA3_FA4, (PCODE)Load_FA1_FA2_FA3_FA4_FA5, (PCODE)Load_FA1_FA2_FA3_FA4_FA5_FA6, (PCODE)Load_FA1_FA2_FA3_FA4_FA5_FA6_FA7,
        (PCODE)0, (PCODE)0, (PCODE)Load_FA2, (PCODE)Load_FA2_FA3, (PCODE)Load_FA2_FA3_FA4, (PCODE)Load_FA2_FA3_FA4_FA5, (PCODE)Load_FA2_FA3_FA4_FA5_FA6, (PCODE)Load_FA2_FA3_FA4_FA5_FA6_FA7,
        (PCODE)0, (PCODE)0, (PCODE)0, (PCODE)Load_FA3, (PCODE)Load_FA3_FA4, (PCODE)Load_FA3_FA4_FA5, (PCODE)Load_FA3_FA4_FA5_FA6, (PCODE)Load_FA3_FA4_FA5_FA6_FA7,
        (PCODE)0, (PCODE)0, (PCODE)0, (PCODE)0, (PCODE)Load_FA4, (PCODE)Load_FA4_FA5, (PCODE)Load_FA4_FA5_FA6, (PCODE)Load_FA4_FA5_FA6_FA7,
        (PCODE)0, (PCODE)0, (PCODE)0, (PCODE)0, (PCODE)0, (PCODE)Load_FA5, (PCODE)Load_FA5_FA6, (PCODE)Load_FA5_FA6_FA7,
        (PCODE)0, (PCODE)0, (PCODE)0, (PCODE)0, (PCODE)0, (PCODE)0, (PCODE)Load_FA6, (PCODE)Load_FA6_FA7,
        (PCODE)0, (PCODE)0, (PCODE)0, (PCODE)0, (PCODE)0, (PCODE)0, (PCODE)0, (PCODE)Load_FA7
    };
    static const PCODE FPRegsStoreRoutines[] = {
        (PCODE)Store_FA0, (PCODE)Store_FA0_FA1, (PCODE)Store_FA0_FA1_FA2, (PCODE)Store_FA0_FA1_FA2_FA3, (PCODE)Store_FA0_FA1_FA2_FA3_FA4, (PCODE)Store_FA0_FA1_FA2_FA3_FA4_FA5, (PCODE)Store_FA0_FA1_FA2_FA3_FA4_FA5_FA6, (PCODE)Store_FA0_FA1_FA2_FA3_FA4_FA5_FA6_FA7,
        (PCODE)0, (PCODE)Store_FA1, (PCODE)Store_FA1_FA2, (PCODE)Store_FA1_FA2_FA3, (PCODE)Store_FA1_FA2_FA3_FA4, (PCODE)Store_FA1_FA2_FA3_FA4_FA5, (PCODE)Store_FA1_FA2_FA3_FA4_FA5_FA6, (PCODE)Store_FA1_FA2_FA3_FA4_FA5_FA6_FA7,
        (PCODE)0, (PCODE)0, (PCODE)Store_FA2, (PCODE)Store_FA2_FA3, (PCODE)Store_FA2_FA3_FA4, (PCODE)Store_FA2_FA3_FA4_FA5, (PCODE)Store_FA2_FA3_FA4_FA5_FA6, (PCODE)Store_FA2_FA3_FA4_FA5_FA6_FA7,
        (PCODE)0, (PCODE)0, (PCODE)0, (PCODE)Store_FA3, (PCODE)Store_FA3_FA4, (PCODE)Store_FA3_FA4_FA5, (PCODE)Store_FA3_FA4_FA5_FA6, (PCODE)Store_FA3_FA4_FA5_FA6_FA7,
        (PCODE)0, (PCODE)0, (PCODE)0, (PCODE)0, (PCODE)Store_FA4, (PCODE)Store_FA4_FA5, (PCODE)Store_FA4_FA5_FA6, (PCODE)Store_FA4_FA5_FA6_FA7,
        (PCODE)0, (PCODE)0, (PCODE)0, (PCODE)0, (PCODE)0, (PCODE)Store_FA5, (PCODE)Store_FA5_FA6, (PCODE)Store_FA5_FA6_FA7,
        (PCODE)0, (PCODE)0, (PCODE)0, (PCODE)0, (PCODE)0, (PCODE)0, (PCODE)Store_FA6, (PCODE)Store_FA6_FA7,
        (PCODE)0, (PCODE)0, (PCODE)0, (PCODE)0, (PCODE)0, (PCODE)0, (PCODE)0, (PCODE)Store_FA7
    };
#endif

    int index = x1 * NUM_FLOAT_ARGUMENT_REGISTERS + x2;
    PCODE routine = m_interpreterToNative ? FPRegsLoadRoutines[index] : FPRegsStoreRoutines[index];
    _ASSERTE(routine != 0);
    return routine;
#endif
}

#ifdef TARGET_ARM64
PCODE CallStubGenerator::GetFPReg128RangeRoutine(int x1, int x2)
{
#if LOG_COMPUTE_CALL_STUB
    LOG2((LF2_INTERPRETER, LL_INFO10000, "GetFPReg128RangeRoutine %d %d\n", x1, x2));
#endif
    static const PCODE FPRegs128LoadRoutines[] = {
        (PCODE)Load_Q0, (PCODE)Load_Q0_Q1, (PCODE)Load_Q0_Q1_Q2, (PCODE)Load_Q0_Q1_Q2_Q3, (PCODE)Load_Q0_Q1_Q2_Q3_Q4, (PCODE)Load_Q0_Q1_Q2_Q3_Q4_Q5, (PCODE)Load_Q0_Q1_Q2_Q3_Q4_Q5_Q6, (PCODE)Load_Q0_Q1_Q2_Q3_Q4_Q5_Q6_Q7,
        (PCODE)0, (PCODE)Load_Q1, (PCODE)Load_Q1_Q2, (PCODE)Load_Q1_Q2_Q3, (PCODE)Load_Q1_Q2_Q3_Q4, (PCODE)Load_Q1_Q2_Q3_Q4_Q5, (PCODE)Load_Q1_Q2_Q3_Q4_Q5_Q6, (PCODE)Load_Q1_Q2_Q3_Q4_Q5_Q6_Q7,
        (PCODE)0, (PCODE)0, (PCODE)Load_Q2, (PCODE)Load_Q2_Q3, (PCODE)Load_Q2_Q3_Q4, (PCODE)Load_Q2_Q3_Q4_Q5, (PCODE)Load_Q2_Q3_Q4_Q5_Q6, (PCODE)Load_Q2_Q3_Q4_Q5_Q6_Q7,
        (PCODE)0, (PCODE)0, (PCODE)0, (PCODE)Load_Q3, (PCODE)Load_Q3_Q4, (PCODE)Load_Q3_Q4_Q5, (PCODE)Load_Q3_Q4_Q5_Q6, (PCODE)Load_Q3_Q4_Q5_Q6_Q7,
        (PCODE)0, (PCODE)0, (PCODE)0, (PCODE)0, (PCODE)Load_Q4, (PCODE)Load_Q4_Q5, (PCODE)Load_Q4_Q5_Q6, (PCODE)Load_Q4_Q5_Q6_Q7,
        (PCODE)0, (PCODE)0, (PCODE)0, (PCODE)0, (PCODE)0, (PCODE)Load_Q5, (PCODE)Load_Q5_Q6, (PCODE)Load_Q5_Q6_Q7,
        (PCODE)0, (PCODE)0, (PCODE)0, (PCODE)0, (PCODE)0, (PCODE)0, (PCODE)Load_Q6, (PCODE)Load_Q6_Q7,
        (PCODE)0, (PCODE)0, (PCODE)0, (PCODE)0, (PCODE)0, (PCODE)0, (PCODE)0, (PCODE)Load_Q7
    };
    static const PCODE FPRegs128StoreRoutines[] = {
        (PCODE)Store_Q0, (PCODE)Store_Q0_Q1, (PCODE)Store_Q0_Q1_Q2, (PCODE)Store_Q0_Q1_Q2_Q3, (PCODE)Store_Q0_Q1_Q2_Q3_Q4, (PCODE)Store_Q0_Q1_Q2_Q3_Q4_Q5, (PCODE)Store_Q0_Q1_Q2_Q3_Q4_Q5_Q6, (PCODE)Store_Q0_Q1_Q2_Q3_Q4_Q5_Q6_Q7,
        (PCODE)0, (PCODE)Store_Q1, (PCODE)Store_Q1_Q2, (PCODE)Store_Q1_Q2_Q3, (PCODE)Store_Q1_Q2_Q3_Q4, (PCODE)Store_Q1_Q2_Q3_Q4_Q5, (PCODE)Store_Q1_Q2_Q3_Q4_Q5_Q6, (PCODE)Store_Q1_Q2_Q3_Q4_Q5_Q6_Q7,
        (PCODE)0, (PCODE)0, (PCODE)Store_Q2, (PCODE)Store_Q2_Q3, (PCODE)Store_Q2_Q3_Q4, (PCODE)Store_Q2_Q3_Q4_Q5, (PCODE)Store_Q2_Q3_Q4_Q5_Q6, (PCODE)Store_Q2_Q3_Q4_Q5_Q6_Q7,
        (PCODE)0, (PCODE)0, (PCODE)0, (PCODE)Store_Q3, (PCODE)Store_Q3_Q4, (PCODE)Store_Q3_Q4_Q5, (PCODE)Store_Q3_Q4_Q5_Q6, (PCODE)Store_Q3_Q4_Q5_Q6_Q7,
        (PCODE)0, (PCODE)0, (PCODE)0, (PCODE)0, (PCODE)Store_Q4, (PCODE)Store_Q4_Q5, (PCODE)Store_Q4_Q5_Q6, (PCODE)Store_Q4_Q5_Q6_Q7,
        (PCODE)0, (PCODE)0, (PCODE)0, (PCODE)0, (PCODE)0, (PCODE)Store_Q5, (PCODE)Store_Q5_Q6, (PCODE)Store_Q5_Q6_Q7,
        (PCODE)0, (PCODE)0, (PCODE)0, (PCODE)0, (PCODE)0, (PCODE)0, (PCODE)Store_Q6, (PCODE)Store_Q6_Q7,
        (PCODE)0, (PCODE)0, (PCODE)0, (PCODE)0, (PCODE)0, (PCODE)0, (PCODE)0, (PCODE)Store_Q7
    };

    int index = x1 * NUM_FLOAT_ARGUMENT_REGISTERS + x2;
    PCODE routine = m_interpreterToNative ? FPRegs128LoadRoutines[index] : FPRegs128StoreRoutines[index];
    _ASSERTE(routine != 0);
    return routine;
}

PCODE CallStubGenerator::GetFPReg32RangeRoutine(int x1, int x2)
{
#if LOG_COMPUTE_CALL_STUB
    LOG2((LF2_INTERPRETER, LL_INFO10000, "GetFPReg32RangeRoutine %d %d\n", x1, x2));

#endif
    static const PCODE FPRegs32LoadRoutines[] = {
        (PCODE)Load_S0, (PCODE)Load_S0_S1, (PCODE)Load_S0_S1_S2, (PCODE)Load_S0_S1_S2_S3, (PCODE)Load_S0_S1_S2_S3_S4, (PCODE)Load_S0_S1_S2_S3_S4_S5, (PCODE)Load_S0_S1_S2_S3_S4_S5_S6, (PCODE)Load_S0_S1_S2_S3_S4_S5_S6_S7,
        (PCODE)0, (PCODE)Load_S1, (PCODE)Load_S1_S2, (PCODE)Load_S1_S2_S3, (PCODE)Load_S1_S2_S3_S4, (PCODE)Load_S1_S2_S3_S4_S5, (PCODE)Load_S1_S2_S3_S4_S5_S6, (PCODE)Load_S1_S2_S3_S4_S5_S6_S7,
        (PCODE)0, (PCODE)0, (PCODE)Load_S2, (PCODE)Load_S2_S3, (PCODE)Load_S2_S3_S4, (PCODE)Load_S2_S3_S4_S5, (PCODE)Load_S2_S3_S4_S5_S6, (PCODE)Load_S2_S3_S4_S5_S6_S7,
        (PCODE)0, (PCODE)0, (PCODE)0, (PCODE)Load_S3, (PCODE)Load_S3_S4, (PCODE)Load_S3_S4_S5, (PCODE)Load_S3_S4_S5_S6, (PCODE)Load_S3_S4_S5_S6_S7,
        (PCODE)0, (PCODE)0, (PCODE)0, (PCODE)0, (PCODE)Load_S4, (PCODE)Load_S4_S5, (PCODE)Load_S4_S5_S6, (PCODE)Load_S4_S5_S6_S7,
        (PCODE)0, (PCODE)0, (PCODE)0, (PCODE)0, (PCODE)0, (PCODE)Load_S5, (PCODE)Load_S5_S6, (PCODE)Load_S5_S6_S7,
        (PCODE)0, (PCODE)0, (PCODE)0, (PCODE)0, (PCODE)0, (PCODE)0, (PCODE)Load_S6, (PCODE)Load_S6_S7,
        (PCODE)0, (PCODE)0, (PCODE)0, (PCODE)0, (PCODE)0, (PCODE)0, (PCODE)0, (PCODE)Load_S7
    };
    static const PCODE FPRegs32StoreRoutines[] = {
        (PCODE)Store_S0, (PCODE)Store_S0_S1, (PCODE)Store_S0_S1_S2, (PCODE)Store_S0_S1_S2_S3, (PCODE)Store_S0_S1_S2_S3_S4, (PCODE)Store_S0_S1_S2_S3_S4_S5, (PCODE)Store_S0_S1_S2_S3_S4_S5_S6, (PCODE)Store_S0_S1_S2_S3_S4_S5_S6_S7,
        (PCODE)0, (PCODE)Store_S1, (PCODE)Store_S1_S2, (PCODE)Store_S1_S2_S3, (PCODE)Store_S1_S2_S3_S4, (PCODE)Store_S1_S2_S3_S4_S5, (PCODE)Store_S1_S2_S3_S4_S5_S6, (PCODE)Store_S1_S2_S3_S4_S5_S6_S7,
        (PCODE)0, (PCODE)0, (PCODE)Store_S2, (PCODE)Store_S2_S3, (PCODE)Store_S2_S3_S4, (PCODE)Store_S2_S3_S4_S5, (PCODE)Store_S2_S3_S4_S5_S6, (PCODE)Store_S2_S3_S4_S5_S6_S7,
        (PCODE)0, (PCODE)0, (PCODE)0, (PCODE)Store_S3, (PCODE)Store_S3_S4, (PCODE)Store_S3_S4_S5, (PCODE)Store_S3_S4_S5_S6, (PCODE)Store_S3_S4_S5_S6_S7,
        (PCODE)0, (PCODE)0, (PCODE)0, (PCODE)0, (PCODE)Store_S4, (PCODE)Store_S4_S5, (PCODE)Store_S4_S5_S6, (PCODE)Store_S4_S5_S6_S7,
        (PCODE)0, (PCODE)0, (PCODE)0, (PCODE)0, (PCODE)0, (PCODE)Store_S5, (PCODE)Store_S5_S6, (PCODE)Store_S5_S6_S7,
        (PCODE)0, (PCODE)0, (PCODE)0, (PCODE)0, (PCODE)0, (PCODE)0, (PCODE)Store_S6, (PCODE)Store_S6_S7,
        (PCODE)0, (PCODE)0, (PCODE)0, (PCODE)0, (PCODE)0, (PCODE)0, (PCODE)0, (PCODE)Store_S7
    };

    int index = x1 * NUM_FLOAT_ARGUMENT_REGISTERS + x2;
    return m_interpreterToNative ? FPRegs32LoadRoutines[index] : FPRegs32StoreRoutines[index];
}
#endif // TARGET_ARM64

#ifdef TARGET_ARM
PCODE CallStubGenerator::GetRegRoutine_4B(int r1, int r2)
{
#if LOG_COMPUTE_CALL_STUB
    LOG2((LF2_INTERPRETER, LL_INFO10000, "GetRegRoutine_4B\n"));
#endif
    static const PCODE GPRegLoadRoutines_4B[] = {
        (PCODE)0, (PCODE)Load_R0_R1_4B, (PCODE)0, (PCODE)Load_R0_R1_R2_R3_4B,
        (PCODE)0, (PCODE)0, (PCODE)0, (PCODE)0,
        (PCODE)0, (PCODE)0, (PCODE)0, (PCODE)Load_R2_R3_4B,
        (PCODE)0, (PCODE)0, (PCODE)0, (PCODE)0
    };
    static const PCODE GPRegStoreRoutines_4B[] = {
        (PCODE)0, (PCODE)Store_R0_R1_4B, (PCODE)0, (PCODE)Store_R0_R1_R2_R3_4B,
        (PCODE)0, (PCODE)0, (PCODE)0, (PCODE)0,
        (PCODE)0, (PCODE)0, (PCODE)0, (PCODE)Store_R2_R3_4B,
        (PCODE)0, (PCODE)0, (PCODE)0, (PCODE)0
    };

    int index = r1 * NUM_ARGUMENT_REGISTERS + r2;
    return m_interpreterToNative ? GPRegLoadRoutines_4B[index] : GPRegStoreRoutines_4B[index];
}

PCODE CallStubGenerator::GetStackRoutine_4B()
{
#if LOG_COMPUTE_CALL_STUB
    LOG2((LF2_INTERPRETER, LL_INFO10000, "GetStackRoutine_4B\n"));
#endif
    return m_interpreterToNative ? (PCODE)Load_Stack_4B : (PCODE)Store_Stack_4B;
}
#endif // TARGET_ARM
#if defined(TARGET_APPLE) && defined(TARGET_ARM64)
PCODE CallStubGenerator::GetSwiftSelfRoutine()
{
#if LOG_COMPUTE_CALL_STUB
    LOG2((LF2_INTERPRETER, LL_INFO10000, "GetSwiftSelfRoutine\n"));
#endif
    return (PCODE)Load_SwiftSelf;
}

PCODE CallStubGenerator::GetSwiftSelfByRefRoutine()
{
#if LOG_COMPUTE_CALL_STUB
    LOG2((LF2_INTERPRETER, LL_INFO10000, "GetSwiftSelfByRefRoutine\n"));
#endif
    return (PCODE)Load_SwiftSelf_ByRef;
}

PCODE CallStubGenerator::GetSwiftErrorRoutine()
{
#if LOG_COMPUTE_CALL_STUB
    LOG2((LF2_INTERPRETER, LL_INFO10000, "GetSwiftErrorRoutine\n"));
#endif
    return (PCODE)Load_SwiftError;
}

PCODE CallStubGenerator::GetSwiftIndirectResultRoutine()
{
#if LOG_COMPUTE_CALL_STUB
    LOG2((LF2_INTERPRETER, LL_INFO10000, "GetSwiftIndirectResultRoutine\n"));
#endif
    return (PCODE)Load_SwiftIndirectResult;
}

PCODE CallStubGenerator::GetSwiftLoadGPAtOffsetRoutine(int regIndex)
{
    static PCODE routines[] = {
        (PCODE)Load_X0_AtOffset, (PCODE)Load_X1_AtOffset, (PCODE)Load_X2_AtOffset, (PCODE)Load_X3_AtOffset,
        (PCODE)Load_X4_AtOffset, (PCODE)Load_X5_AtOffset, (PCODE)Load_X6_AtOffset, (PCODE)Load_X7_AtOffset
    };
    _ASSERTE(regIndex >= 0 && regIndex < ARRAY_SIZE(routines));
    return routines[regIndex];
}

PCODE CallStubGenerator::GetSwiftLoadFPAtOffsetRoutine(int regIndex)
{
    static PCODE routines[] = {
        (PCODE)Load_D0_AtOffset, (PCODE)Load_D1_AtOffset, (PCODE)Load_D2_AtOffset, (PCODE)Load_D3_AtOffset,
        (PCODE)Load_D4_AtOffset, (PCODE)Load_D5_AtOffset, (PCODE)Load_D6_AtOffset, (PCODE)Load_D7_AtOffset
    };
    _ASSERTE(regIndex >= 0 && regIndex < ARRAY_SIZE(routines));
    return routines[regIndex];
}

PCODE CallStubGenerator::GetSwiftStoreGPAtOffsetRoutine(int regIndex)
{
    static PCODE routines[] = {
        (PCODE)Store_X0_AtOffset, (PCODE)Store_X1_AtOffset, (PCODE)Store_X2_AtOffset, (PCODE)Store_X3_AtOffset
    };
    _ASSERTE(regIndex >= 0 && regIndex < ARRAY_SIZE(routines));
    return routines[regIndex];
}

PCODE CallStubGenerator::GetSwiftStoreFPAtOffsetRoutine(int regIndex)
{
    static PCODE routines[] = {
        (PCODE)Store_D0_AtOffset, (PCODE)Store_D1_AtOffset, (PCODE)Store_D2_AtOffset, (PCODE)Store_D3_AtOffset
    };
    _ASSERTE(regIndex >= 0 && regIndex < ARRAY_SIZE(routines));
    return routines[regIndex];
}
#endif // TARGET_APPLE && TARGET_ARM64

extern "C" void CallJittedMethodRetVoid(PCODE *routines, int8_t*pArgs, int8_t*pRet, int totalStackSize, PTR_PTR_Object pContinuation);
extern "C" void CallJittedMethodRetDouble(PCODE *routines, int8_t*pArgs, int8_t*pRet, int totalStackSize, PTR_PTR_Object pContinuation);
extern "C" void CallJittedMethodRetFloat(PCODE *routines, int8_t*pArgs, int8_t*pRet, int totalStackSize, PTR_PTR_Object pContinuation);
extern "C" void CallJittedMethodRetI1(PCODE *routines, int8_t*pArgs, int8_t*pRet, int totalStackSize, PTR_PTR_Object pContinuation);
extern "C" void CallJittedMethodRetU1(PCODE *routines, int8_t*pArgs, int8_t*pRet, int totalStackSize, PTR_PTR_Object pContinuation);
extern "C" void CallJittedMethodRetI2(PCODE *routines, int8_t*pArgs, int8_t*pRet, int totalStackSize, PTR_PTR_Object pContinuation);
extern "C" void CallJittedMethodRetU2(PCODE *routines, int8_t*pArgs, int8_t*pRet, int totalStackSize, PTR_PTR_Object pContinuation);
extern "C" void CallJittedMethodRetI8(PCODE *routines, int8_t*pArgs, int8_t*pRet, int totalStackSize, PTR_PTR_Object pContinuation);
#ifdef TARGET_32BIT
extern "C" void CallJittedMethodRetI4(PCODE *routines, int8_t*pArgs, int8_t*pRet, int totalStackSize, PTR_PTR_Object pContinuation);
extern "C" void CallJittedMethodRetFloat(PCODE *routines, int8_t*pArgs, int8_t*pRet, int totalStackSize, PTR_PTR_Object pContinuation);
#endif // TARGET_32BIT
extern "C" void InterpreterStubRetVoid();
extern "C" void InterpreterStubRetDouble();
extern "C" void InterpreterStubRetI8();
#ifdef TARGET_32BIT
extern "C" void InterpreterStubRetI4();
extern "C" void InterpreterStubRetFloat();
#endif // TARGET_32BIT

#ifdef TARGET_AMD64
#ifdef TARGET_WINDOWS
extern "C" void CallJittedMethodRetBuffRCX(PCODE *routines, int8_t*pArgs, int8_t*pRet, int totalStackSize, PTR_PTR_Object pContinuation);
extern "C" void CallJittedMethodRetBuffRDX(PCODE *routines, int8_t*pArgs, int8_t*pRet, int totalStackSize, PTR_PTR_Object pContinuation);
extern "C" void InterpreterStubRetBuffRCX();
extern "C" void InterpreterStubRetBuffRDX();
#else // TARGET_WINDOWS
extern "C" void CallJittedMethodRetBuffRDI(PCODE *routines, int8_t*pArgs, int8_t*pRet, int totalStackSize, PTR_PTR_Object pContinuation);
extern "C" void CallJittedMethodRetBuffRSI(PCODE *routines, int8_t*pArgs, int8_t*pRet, int totalStackSize, PTR_PTR_Object pContinuation);
extern "C" void InterpreterStubRetBuffRDI();
extern "C" void InterpreterStubRetBuffRSI();
#endif // TARGET_WINDOWS
#elif defined(TARGET_ARM) // TARGET_ARM
extern "C" void CallJittedMethodRetBuffR0(PCODE *routines, int8_t*pArgs, int8_t*pRet, int totalStackSize, PTR_PTR_Object pContinuation);
extern "C" void CallJittedMethodRetBuffR1(PCODE *routines, int8_t*pArgs, int8_t*pRet, int totalStackSize, PTR_PTR_Object pContinuation);
extern "C" void InterpreterStubRetBuffR0();
extern "C" void InterpreterStubRetBuffR1();
#else // !TARGET_AMD64 && !TARGET_ARM
#if defined(TARGET_ARM64) && defined(TARGET_WINDOWS)
extern "C" void CallJittedMethodRetBuffX1(PCODE *routines, int8_t*pArgs, int8_t*pRet, int totalStackSize, PTR_PTR_Object pContinuation);
extern "C" void InterpreterStubRetBuffX1();
#endif
extern "C" void CallJittedMethodRetBuff(PCODE *routines, int8_t*pArgs, int8_t*pRet, int totalStackSize, PTR_PTR_Object pContinuation);
extern "C" void InterpreterStubRetBuff();
#endif // TARGET_AMD64

#ifdef UNIX_AMD64_ABI
extern "C" void CallJittedMethodRetI8I8(PCODE *routines, int8_t*pArgs, int8_t*pRet, int totalStackSize, PTR_PTR_Object pContinuation);
extern "C" void CallJittedMethodRetI8Double(PCODE *routines, int8_t*pArgs, int8_t*pRet, int totalStackSize, PTR_PTR_Object pContinuation);
extern "C" void CallJittedMethodRetDoubleI8(PCODE *routines, int8_t*pArgs, int8_t*pRet, int totalStackSize, PTR_PTR_Object pContinuation);
extern "C" void CallJittedMethodRetDoubleDouble(PCODE *routines, int8_t*pArgs, int8_t*pRet, int totalStackSize, PTR_PTR_Object pContinuation);
extern "C" void InterpreterStubRetI8I8();
extern "C" void InterpreterStubRetI8Double();
extern "C" void InterpreterStubRetDoubleI8();
extern "C" void InterpreterStubRetDoubleDouble();
#endif

#ifdef TARGET_ARM64
extern "C" void CallJittedMethodRet2I8(PCODE *routines, int8_t*pArgs, int8_t*pRet, int totalStackSize, PTR_PTR_Object pContinuation);
extern "C" void CallJittedMethodRet2Double(PCODE *routines, int8_t*pArgs, int8_t*pRet, int totalStackSize, PTR_PTR_Object pContinuation);
extern "C" void CallJittedMethodRet3Double(PCODE *routines, int8_t*pArgs, int8_t*pRet, int totalStackSize, PTR_PTR_Object pContinuation);
extern "C" void CallJittedMethodRet4Double(PCODE *routines, int8_t*pArgs, int8_t*pRet, int totalStackSize, PTR_PTR_Object pContinuation);
extern "C" void CallJittedMethodRet2Float(PCODE *routines, int8_t*pArgs, int8_t*pRet, int totalStackSize, PTR_PTR_Object pContinuation);
extern "C" void CallJittedMethodRet3Float(PCODE *routines, int8_t*pArgs, int8_t*pRet, int totalStackSize, PTR_PTR_Object pContinuation);
extern "C" void CallJittedMethodRet4Float(PCODE *routines, int8_t*pArgs, int8_t*pRet, int totalStackSize, PTR_PTR_Object pContinuation);
extern "C" void CallJittedMethodRetVector64(PCODE *routines, int8_t *pArgs, int8_t *pRet, int totalStackSize, PTR_PTR_Object pContinuation);
extern "C" void CallJittedMethodRet2Vector64(PCODE *routines, int8_t *pArgs, int8_t *pRet, int totalStackSize, PTR_PTR_Object pContinuation);
extern "C" void CallJittedMethodRet3Vector64(PCODE *routines, int8_t *pArgs, int8_t *pRet, int totalStackSize, PTR_PTR_Object pContinuation);
extern "C" void CallJittedMethodRet4Vector64(PCODE *routines, int8_t *pArgs, int8_t *pRet, int totalStackSize, PTR_PTR_Object pContinuation);
extern "C" void CallJittedMethodRetVector128(PCODE *routines, int8_t *pArgs, int8_t *pRet, int totalStackSize, PTR_PTR_Object pContinuation);
extern "C" void CallJittedMethodRet2Vector128(PCODE *routines, int8_t *pArgs, int8_t *pRet, int totalStackSize, PTR_PTR_Object pContinuation);
extern "C" void CallJittedMethodRet3Vector128(PCODE *routines, int8_t *pArgs, int8_t *pRet, int totalStackSize, PTR_PTR_Object pContinuation);
extern "C" void CallJittedMethodRet4Vector128(PCODE *routines, int8_t *pArgs, int8_t *pRet, int totalStackSize, PTR_PTR_Object pContinuation);
#if defined(TARGET_APPLE)
extern "C" void CallJittedMethodRetSwiftLowered(PCODE *routines, int8_t*pArgs, int8_t*pRet, int totalStackSize, PTR_PTR_Object pContinuation);
#endif // TARGET_APPLE
extern "C" void InterpreterStubRet2I8();
extern "C" void InterpreterStubRet2Double();
extern "C" void InterpreterStubRet3Double();
extern "C" void InterpreterStubRet4Double();
extern "C" void InterpreterStubRetFloat();
extern "C" void InterpreterStubRet2Float();
extern "C" void InterpreterStubRet3Float();
extern "C" void InterpreterStubRet4Float();
extern "C" void InterpreterStubRetVector64();
extern "C" void InterpreterStubRet2Vector64();
extern "C" void InterpreterStubRet3Vector64();
extern "C" void InterpreterStubRet4Vector64();
extern "C" void InterpreterStubRetVector128();
extern "C" void InterpreterStubRet2Vector128();
extern "C" void InterpreterStubRet3Vector128();
extern "C" void InterpreterStubRet4Vector128();
#endif // TARGET_ARM64

#if defined(TARGET_RISCV64)
extern "C" void CallJittedMethodRet2I8(PCODE *routines, int8_t*pArgs, int8_t*pRet, int totalStackSize, PTR_PTR_Object pContinuation);
extern "C" void CallJittedMethodRet2Double(PCODE *routines, int8_t*pArgs, int8_t*pRet, int totalStackSize, PTR_PTR_Object pContinuation);
extern "C" void CallJittedMethodRetFloatInt(PCODE *routines, int8_t*pArgs, int8_t*pRet, int totalStackSize, PTR_PTR_Object pContinuation);
extern "C" void CallJittedMethodRetIntFloat(PCODE *routines, int8_t*pArgs, int8_t*pRet, int totalStackSize, PTR_PTR_Object pContinuation);
extern "C" void InterpreterStubRet2I8();
extern "C" void InterpreterStubRet2Double();
extern "C" void InterpreterStubRetFloatInt();
extern "C" void InterpreterStubRetIntFloat();
#endif // TARGET_RISCV64

#define INVOKE_FUNCTION_PTR(functionPtrName) LOG2((LF2_INTERPRETER, LL_INFO10000, #functionPtrName "\n")); return functionPtrName

CallStubHeader::InvokeFunctionPtr CallStubGenerator::GetInvokeFunctionPtr(CallStubGenerator::ReturnType returnType)
{
    STANDARD_VM_CONTRACT;

    switch (returnType)
    {
        case ReturnTypeVoid:
            INVOKE_FUNCTION_PTR(CallJittedMethodRetVoid);
        case ReturnTypeDouble:
#ifndef ARM_SOFTFP
            INVOKE_FUNCTION_PTR(CallJittedMethodRetDouble);
#endif // !ARM_SOFTFP
        case ReturnTypeI8:
            INVOKE_FUNCTION_PTR(CallJittedMethodRetI8);
#ifdef TARGET_32BIT
        case ReturnTypeFloat:
#ifndef ARM_SOFTFP
            INVOKE_FUNCTION_PTR(CallJittedMethodRetFloat);
#endif // !ARM_SOFTFP
        case ReturnTypeI4:
            INVOKE_FUNCTION_PTR(CallJittedMethodRetI4);
#endif // TARGET_32BIT
        case ReturnTypeI1:
            INVOKE_FUNCTION_PTR(CallJittedMethodRetI1);
        case ReturnTypeU1:
            INVOKE_FUNCTION_PTR(CallJittedMethodRetU1);
        case ReturnTypeI2:
            INVOKE_FUNCTION_PTR(CallJittedMethodRetI2);
        case ReturnTypeU2:
            INVOKE_FUNCTION_PTR(CallJittedMethodRetU2);
#ifdef TARGET_AMD64
#ifdef TARGET_WINDOWS
        case ReturnTypeBuffArg1:
            INVOKE_FUNCTION_PTR(CallJittedMethodRetBuffRCX);
        case ReturnTypeBuffArg2:
            INVOKE_FUNCTION_PTR(CallJittedMethodRetBuffRDX);
#else // TARGET_WINDOWS
        case ReturnTypeBuffArg1:
            INVOKE_FUNCTION_PTR(CallJittedMethodRetBuffRDI);
        case ReturnTypeBuffArg2:
            INVOKE_FUNCTION_PTR(CallJittedMethodRetBuffRSI);
#endif // TARGET_WINDOWS
#elif defined(TARGET_ARM64) && defined(TARGET_WINDOWS)
        case ReturnTypeBuffArg2:
            INVOKE_FUNCTION_PTR(CallJittedMethodRetBuffX1);
        case ReturnTypeBuff:
            INVOKE_FUNCTION_PTR(CallJittedMethodRetBuff);
#elif defined(TARGET_ARM)
        case ReturnTypeBuffArg1:
            INVOKE_FUNCTION_PTR(CallJittedMethodRetBuffR0);
        case ReturnTypeBuffArg2:
            INVOKE_FUNCTION_PTR(CallJittedMethodRetBuffR1);
#else // !TARGET_AMD64 && !TARGET_ARM && !(TARGET_ARM64 && TARGET_WINDOWS)
        case ReturnTypeBuff:
            INVOKE_FUNCTION_PTR(CallJittedMethodRetBuff);
#endif // TARGET_AMD64
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
        case ReturnTypeVector64:
            INVOKE_FUNCTION_PTR(CallJittedMethodRetVector64);
        case ReturnType2Vector64:
            INVOKE_FUNCTION_PTR(CallJittedMethodRet2Vector64);
        case ReturnType3Vector64:
            INVOKE_FUNCTION_PTR(CallJittedMethodRet3Vector64);
        case ReturnType4Vector64:
            INVOKE_FUNCTION_PTR(CallJittedMethodRet4Vector64);
        case ReturnTypeVector128:
            INVOKE_FUNCTION_PTR(CallJittedMethodRetVector128);
        case ReturnType2Vector128:
            INVOKE_FUNCTION_PTR(CallJittedMethodRet2Vector128);
        case ReturnType3Vector128:
            INVOKE_FUNCTION_PTR(CallJittedMethodRet3Vector128);
        case ReturnType4Vector128:
            INVOKE_FUNCTION_PTR(CallJittedMethodRet4Vector128);
#if defined(TARGET_APPLE)
        case ReturnTypeSwiftLowered:
            INVOKE_FUNCTION_PTR(CallJittedMethodRetSwiftLowered);
#endif // TARGET_APPLE
#endif // TARGET_ARM64
#if defined(TARGET_RISCV64)
        case ReturnType2I8:
            INVOKE_FUNCTION_PTR(CallJittedMethodRet2I8);
        case ReturnType2Double:
            INVOKE_FUNCTION_PTR(CallJittedMethodRet2Double);
        case ReturnTypeFloatInt:
            INVOKE_FUNCTION_PTR(CallJittedMethodRetFloatInt);
        case ReturnTypeIntFloat:
            INVOKE_FUNCTION_PTR(CallJittedMethodRetIntFloat);
#endif // TARGET_RISCV64
        default:
            _ASSERTE(!"Unexpected return type for interpreter stub");
            return NULL; // This should never happen, but just in case.
    }
}

#define RETURN_TYPE_HANDLER(returnType) LOG2((LF2_INTERPRETER, LL_INFO10000, #returnType "\n")); return (PCODE)returnType

PCODE CallStubGenerator::GetInterpreterReturnTypeHandler(CallStubGenerator::ReturnType returnType)
{
    STANDARD_VM_CONTRACT;

    switch (returnType)
    {
        case ReturnTypeVoid:
            RETURN_TYPE_HANDLER(InterpreterStubRetVoid);
        case ReturnTypeDouble:
#ifndef ARM_SOFTFP
            RETURN_TYPE_HANDLER(InterpreterStubRetDouble);
#endif // !ARM_SOFTFP
        case ReturnTypeI1:
        case ReturnTypeU1:
        case ReturnTypeI8:
        case ReturnTypeI2:
        case ReturnTypeU2:
            RETURN_TYPE_HANDLER(InterpreterStubRetI8);
#ifdef TARGET_32BIT
        case ReturnTypeFloat:
#ifndef ARM_SOFTFP
            RETURN_TYPE_HANDLER(InterpreterStubRetFloat);
#endif // !ARM_SOFTFP
        case ReturnTypeI4:
            RETURN_TYPE_HANDLER(InterpreterStubRetI4);
#endif // TARGET_32BIT
#ifdef TARGET_AMD64
        case ReturnTypeBuffArg1:
#ifdef TARGET_WINDOWS
            RETURN_TYPE_HANDLER(InterpreterStubRetBuffRCX);
#else
            RETURN_TYPE_HANDLER(InterpreterStubRetBuffRDI);
#endif
        case ReturnTypeBuffArg2:
#ifdef TARGET_WINDOWS
            RETURN_TYPE_HANDLER(InterpreterStubRetBuffRDX);
#else
            RETURN_TYPE_HANDLER(InterpreterStubRetBuffRSI);
#endif
#elif defined(TARGET_ARM64) && defined(TARGET_WINDOWS)
        case ReturnTypeBuffArg2:
            RETURN_TYPE_HANDLER(InterpreterStubRetBuffX1);
        case ReturnTypeBuff:
            RETURN_TYPE_HANDLER(InterpreterStubRetBuff);
#elif defined(TARGET_ARM)
        case ReturnTypeBuffArg1:
            RETURN_TYPE_HANDLER(InterpreterStubRetBuffR0);
        case ReturnTypeBuffArg2:
            RETURN_TYPE_HANDLER(InterpreterStubRetBuffR1);
#else // !TARGET_AMD64 && !TARGET_ARM && !(TARGET_ARM64 && TARGET_WINDOWS)
        case ReturnTypeBuff:
            RETURN_TYPE_HANDLER(InterpreterStubRetBuff);
#endif // TARGET_AMD64
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
        case ReturnTypeVector64:
            RETURN_TYPE_HANDLER(InterpreterStubRetVector64);
        case ReturnType2Vector64:
            RETURN_TYPE_HANDLER(InterpreterStubRet2Vector64);
        case ReturnType3Vector64:
            RETURN_TYPE_HANDLER(InterpreterStubRet3Vector64);
        case ReturnType4Vector64:
            RETURN_TYPE_HANDLER(InterpreterStubRet4Vector64);
        case ReturnTypeVector128:
            RETURN_TYPE_HANDLER(InterpreterStubRetVector128);
        case ReturnType2Vector128:
            RETURN_TYPE_HANDLER(InterpreterStubRet2Vector128);
        case ReturnType3Vector128:
            RETURN_TYPE_HANDLER(InterpreterStubRet3Vector128);
        case ReturnType4Vector128:
            RETURN_TYPE_HANDLER(InterpreterStubRet4Vector128);
#endif // TARGET_ARM64
#if defined(TARGET_RISCV64)
        case ReturnType2I8:
            RETURN_TYPE_HANDLER(InterpreterStubRet2I8);
        case ReturnType2Double:
            RETURN_TYPE_HANDLER(InterpreterStubRet2Double);
        case ReturnTypeFloatInt:
            RETURN_TYPE_HANDLER(InterpreterStubRetFloatInt);
        case ReturnTypeIntFloat:
            RETURN_TYPE_HANDLER(InterpreterStubRetIntFloat);
#endif // TARGET_RISCV64
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

    LOG2((LF2_INTERPRETER, LL_INFO10000, "GenerateCallStub interpreterToNative=%d\n", interpreterToNative ? 1 : 0));
    m_interpreterToNative = interpreterToNative;

    MetaSig sig(pMD);
    // Allocate space for the routines. The size of the array is conservatively set to twice the number of arguments
    // plus one slot for the target pointer and reallocated to the real size at the end.
    size_t tempStorageSize = ComputeTempStorageSize(sig);
    PCODE *pRoutines = (PCODE*)alloca(tempStorageSize);
    memset(pRoutines, 0, tempStorageSize);

    ComputeCallStub(sig, pRoutines, pMD);

    LoaderAllocator *pLoaderAllocator = pMD->GetLoaderAllocator();
    S_SIZE_T finalStubSize(sizeof(CallStubHeader) + m_routineIndex * sizeof(PCODE));
    void *pHeaderStorage = pamTracker->Track(pLoaderAllocator->GetHighFrequencyHeap()->AllocMem(finalStubSize));
    bool hasSwiftError = m_isSwiftCallConv && m_hasSwiftError && pMD->IsILStub();

    int targetSlotIndex = m_interpreterToNative ? m_targetSlotIndex : (m_routineIndex - 1);
    CallStubHeader *pHeader = new (pHeaderStorage) CallStubHeader(m_routineIndex, targetSlotIndex, pRoutines, ALIGN_UP(m_totalStackSize, STACK_ALIGN_SIZE), sig.IsAsyncCall(), hasSwiftError, m_pInvokeFunction);

    return pHeader;
}

struct CachedCallStubKey
{
    CachedCallStubKey(int32_t hashCode, int numRoutines, int targetSlotIndex, PCODE *pRoutines, int totalStackSize, bool hasContinuationRet, bool hasSwiftError, CallStubHeader::InvokeFunctionPtr pInvokeFunction)
     : HashCode(hashCode), NumRoutines(numRoutines), TargetSlotIndex(targetSlotIndex), TotalStackSize(totalStackSize), HasContinuationRet(hasContinuationRet), HasSwiftError(hasSwiftError), Invoke(pInvokeFunction), Routines(pRoutines)
    {
    }

    bool operator==(const CachedCallStubKey& other) const
    {
        LIMITED_METHOD_CONTRACT;

        if (HashCode != other.HashCode || NumRoutines != other.NumRoutines || TargetSlotIndex != other.TargetSlotIndex || TotalStackSize != other.TotalStackSize || Invoke != other.Invoke || HasContinuationRet != other.HasContinuationRet || HasSwiftError != other.HasSwiftError)
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
    const int TargetSlotIndex = 0;
    const int TotalStackSize = 0;
    const bool HasContinuationRet = false;
    const bool HasSwiftError = false;
    const CallStubHeader::InvokeFunctionPtr Invoke = NULL; // Pointer to the invoke function
    const PCODE *Routines;
};

struct CachedCallStub
{
    CachedCallStub(int32_t hashCode, int numRoutines, int targetSlotIndex, PCODE *pRoutines, int totalStackSize, bool hasContinuationRet, bool hasSwiftError, CallStubHeader::InvokeFunctionPtr pInvokeFunction) :
        HashCode(hashCode),
        Header(numRoutines, targetSlotIndex, pRoutines, totalStackSize, hasContinuationRet, hasSwiftError, pInvokeFunction)
    {
    }

    int32_t HashCode;
    CallStubHeader Header;

    CachedCallStubKey GetKey()
    {
        return CachedCallStubKey(
            HashCode,
            Header.NumRoutines,
            Header.TargetSlotIndex,
            &Header.Routines[0],
            Header.TotalStackSize,
            Header.HasContinuationRet,
            Header.HasSwiftError,
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

    // Allocate space for the routines. The size of the array is conservatively set to three times the number of arguments
    // plus one slot for the target pointer and reallocated to the real size at the end.
    size_t tempStorageSize = ComputeTempStorageSize(sig);
    PCODE *pRoutines = (PCODE*)alloca(tempStorageSize);
    memset(pRoutines, 0, tempStorageSize);

    m_interpreterToNative = true; // We always generate the interpreter to native call stub here

    ComputeCallStub(sig, pRoutines, NULL);

    xxHash hashState;
    for (int i = 0; i < m_routineIndex; i++)
    {
        hashState.AddPointer((void*)pRoutines[i]);
    }
    hashState.Add(m_totalStackSize);
    hashState.AddPointer((void*)m_pInvokeFunction);
    hashState.Add(sig.IsAsyncCall() ? 1 : 0);
    hashState.Add(m_targetSlotIndex);
    hashState.Add(m_hasSwiftError ? 1 : 0);

    CachedCallStubKey cachedHeaderKey(
        hashState.ToHashCode(),
        m_routineIndex,
        m_targetSlotIndex,
        pRoutines,
        ALIGN_UP(m_totalStackSize, STACK_ALIGN_SIZE),
        sig.IsAsyncCall(),
        m_hasSwiftError,
        m_pInvokeFunction);

    CrstHolder lockHolder(&s_callStubCrst);
    CachedCallStub *pCachedHeader = s_callStubCache->Lookup(cachedHeaderKey);
    if (pCachedHeader != NULL)
    {
        // The stub is already cached, return the cached header
        LOG2((LF2_INTERPRETER, LL_INFO10000, "CallStubHeader at %p\n", &pCachedHeader->Header));
        return &pCachedHeader->Header;
    }
    else
    {
        AllocMemTracker amTracker;
        // The stub is not cached, create a new header and add it to the cache
        // We only need to allocate the actual pRoutines array, and then we can just use the cachedHeader we already constructed
        size_t finalCachedCallStubSize = sizeof(CachedCallStub) + m_routineIndex * sizeof(PCODE);
        void* pHeaderStorage = amTracker.Track(SystemDomain::GetGlobalLoaderAllocator()->GetHighFrequencyHeap()->AllocMem(S_SIZE_T(finalCachedCallStubSize)));
        CachedCallStub *pHeader = new (pHeaderStorage) CachedCallStub(cachedHeaderKey.HashCode, m_routineIndex, m_targetSlotIndex, pRoutines, ALIGN_UP(m_totalStackSize, STACK_ALIGN_SIZE), sig.IsAsyncCall(), m_hasSwiftError, m_pInvokeFunction);
        s_callStubCache->Add(pHeader);
        amTracker.SuppressRelease();

        _ASSERTE(s_callStubCache->Lookup(cachedHeaderKey) == pHeader);
        LOG2((LF2_INTERPRETER, LL_INFO10000, "CallStubHeader at %p\n", &pHeader->Header));
        return &pHeader->Header;
    }
};

void CallStubGenerator::TerminateCurrentRoutineIfNotOfNewType(RoutineType type, PCODE *pRoutines)
{
    if ((m_currentRoutineType == RoutineType::GPReg) && (type != RoutineType::GPReg))
    {
        pRoutines[m_routineIndex++] = GetGPRegRangeRoutine(m_r1, m_r2);
        m_r1 = NoRange;
        m_currentRoutineType = RoutineType::None;
    }
    else if ((m_currentRoutineType == RoutineType::FPReg) && (type != RoutineType::FPReg))
    {
        pRoutines[m_routineIndex++] = GetFPRegRangeRoutine(m_x1, m_x2);
        m_x1 = NoRange;
        m_currentRoutineType = RoutineType::None;
    }
#ifdef TARGET_ARM64
    else if ((m_currentRoutineType == RoutineType::FPReg32) && (type != RoutineType::FPReg32))
    {
        pRoutines[m_routineIndex++] = GetFPReg32RangeRoutine(m_x1, m_x2);
        m_x1 = NoRange;
        m_currentRoutineType = RoutineType::None;
    }
    else if ((m_currentRoutineType == RoutineType::FPReg128) && (type != RoutineType::FPReg128))
    {
        pRoutines[m_routineIndex++] = GetFPReg128RangeRoutine(m_x1, m_x2);
        m_x1 = NoRange;
        m_currentRoutineType = RoutineType::None;
    }
#if defined(TARGET_APPLE)
    else if ((m_currentRoutineType == RoutineType::SwiftSelf) && (type != RoutineType::SwiftSelf))
    {
        pRoutines[m_routineIndex++] = GetSwiftSelfRoutine();
        m_currentRoutineType = RoutineType::None;
    }
    else if ((m_currentRoutineType == RoutineType::SwiftSelfByRef) && (type != RoutineType::SwiftSelfByRef))
    {
        pRoutines[m_routineIndex++] = GetSwiftSelfByRefRoutine();
        pRoutines[m_routineIndex++] = (PCODE)m_swiftSelfByRefSize;
        m_swiftSelfByRefSize = 0;
        m_currentRoutineType = RoutineType::None;
    }
    else if ((m_currentRoutineType == RoutineType::SwiftError) && (type != RoutineType::SwiftError))
    {
        pRoutines[m_routineIndex++] = GetSwiftErrorRoutine();
        m_currentRoutineType = RoutineType::None;
    }
    else if ((m_currentRoutineType == RoutineType::SwiftIndirectResult) && (type != RoutineType::SwiftIndirectResult))
    {
        pRoutines[m_routineIndex++] = GetSwiftIndirectResultRoutine();
        m_currentRoutineType = RoutineType::None;
    }
#endif // TARGET_APPLE
#endif // TARGET_ARM64
    else if ((m_currentRoutineType == RoutineType::Stack) && (type != RoutineType::Stack))
    {
        pRoutines[m_routineIndex++] = GetStackRoutine();
#ifdef TARGET_32BIT
        pRoutines[m_routineIndex++] = m_s1;
        pRoutines[m_routineIndex++] = m_s2 - m_s1 + 1;
#else // !TARGET_32BIT
        pRoutines[m_routineIndex++] = ((int64_t)(m_s2 - m_s1 + 1) << 32) | m_s1;
#endif // TARGET_32BIT
        m_s1 = NoRange;
        m_currentRoutineType = RoutineType::None;
    }

    return;
}

//---------------------------------------------------------------------------
// isNativePrimitiveStructType:
//    Check if the given struct type is an intrinsic type that should be treated as though
//    it is not a struct at the unmanaged ABI boundary.
//
// Arguments:
//    pMT - the handle for the struct type.
//
// Return Value:
//    true if the given struct type should be treated as a primitive for unmanaged calls,
//    false otherwise.
//
bool isNativePrimitiveStructType(MethodTable* pMT)
{
    if (!pMT->IsIntrinsicType())
    {
        return false;
    }
    const char* namespaceName = nullptr;
    const char* typeName      = pMT->GetFullyQualifiedNameInfo(&namespaceName);

    if ((namespaceName == NULL) || (typeName == NULL))
    {
        return false;
    }

    if (strcmp(namespaceName, "System.Runtime.InteropServices") != 0)
    {
        return false;
    }

    return strcmp(typeName, "CLong") == 0 || strcmp(typeName, "CULong") == 0 || strcmp(typeName, "NFloat") == 0;
}

void CallStubGenerator::ComputeCallStub(MetaSig &sig, PCODE *pRoutines, MethodDesc *pMD)
{
    bool hasUnmanagedCallConv = false;
    CorInfoCallConvExtension unmanagedCallConv = CorInfoCallConvExtension::C;

    if (pMD != NULL && (pMD->IsPInvoke()))
    {
        PInvoke::GetCallingConvention_IgnoreErrors(pMD, &unmanagedCallConv, NULL);
        hasUnmanagedCallConv = true;
    }
    else if (pMD != NULL && pMD->IsILStub())
    {
        MethodDesc* pTargetMD = pMD->AsDynamicMethodDesc()->GetILStubResolver()->GetStubTargetMethodDesc();
        if (pTargetMD != NULL && pTargetMD->IsPInvoke())
        {
            PInvoke::GetCallingConvention_IgnoreErrors(pTargetMD, &unmanagedCallConv, NULL);
            hasUnmanagedCallConv = true;
        }
    }
    else if (pMD != NULL && pMD->HasUnmanagedCallersOnlyAttribute())
    {
        if (CallConv::TryGetCallingConventionFromUnmanagedCallersOnly(pMD, &unmanagedCallConv))
        {
            if (sig.GetCallingConvention() == IMAGE_CEE_CS_CALLCONV_VARARG)
            {
                unmanagedCallConv = CorInfoCallConvExtension::C;
            }
        }
        else
        {
            unmanagedCallConv = CallConv::GetDefaultUnmanagedCallingConvention();
        }
        hasUnmanagedCallConv = true;
    }
    else
    {
        switch (sig.GetCallingConvention())
        {
            case IMAGE_CEE_CS_CALLCONV_THISCALL:
                unmanagedCallConv = CorInfoCallConvExtension::Thiscall;
                hasUnmanagedCallConv = true;
                break;
            case IMAGE_CEE_UNMANAGED_CALLCONV_C:
                unmanagedCallConv = CorInfoCallConvExtension::C;
                hasUnmanagedCallConv = true;
                break;
            case IMAGE_CEE_UNMANAGED_CALLCONV_STDCALL:
                unmanagedCallConv = CorInfoCallConvExtension::Stdcall;
                hasUnmanagedCallConv = true;
                break;
            case IMAGE_CEE_UNMANAGED_CALLCONV_FASTCALL:
                unmanagedCallConv = CorInfoCallConvExtension::Fastcall;
                hasUnmanagedCallConv = true;
                break;
            case IMAGE_CEE_CS_CALLCONV_UNMANAGED:
                unmanagedCallConv = GetUnmanagedCallConvExtension(&sig);
                hasUnmanagedCallConv = true;
                break;
        }
    }

    if (hasUnmanagedCallConv)
    {
#if defined(TARGET_ARM64) && defined(TARGET_WINDOWS)
        if (callConvIsInstanceMethodCallConv(unmanagedCallConv))
        {
            ComputeCallStubWorker<WindowsArm64PInvokeThisCallArgIterator>(hasUnmanagedCallConv, unmanagedCallConv, sig, pRoutines, pMD);
        }
        else
#endif // defined(TARGET_ARM64) && defined(TARGET_WINDOWS)
        {
            ComputeCallStubWorker<PInvokeArgIterator>(hasUnmanagedCallConv, unmanagedCallConv, sig, pRoutines, pMD);
        }
    }
    else
    {
        ComputeCallStubWorker<ArgIterator>(hasUnmanagedCallConv, unmanagedCallConv, sig, pRoutines, pMD);
    }
}

template<typename ArgIteratorType>
void CallStubGenerator::ComputeCallStubWorker(bool hasUnmanagedCallConv, CorInfoCallConvExtension unmanagedCallConv, MetaSig &sig, PCODE *pRoutines, MethodDesc *pMD)
{
    bool unmanagedThisCallConv = false;
    bool rewriteMetaSigFromExplicitThisToHasThis = false;
#if defined(TARGET_APPLE) && defined(TARGET_ARM64)
    bool isSwiftCallConv = false;
#endif

    if (hasUnmanagedCallConv)
    {
#if defined(TARGET_APPLE) && defined(TARGET_ARM64)
        isSwiftCallConv = (unmanagedCallConv == CorInfoCallConvExtension::Swift);
        m_isSwiftCallConv = isSwiftCallConv;
        if (!isSwiftCallConv)
#endif
        {
            unmanagedThisCallConv = callConvIsInstanceMethodCallConv(unmanagedCallConv);
        }
    }

#if defined(TARGET_WINDOWS)
    // On these platforms, when making a ThisCall, or other call using a C++ MemberFunction calling convention,
    // the "this" pointer is passed in the first argument slot.
    bool rewriteReturnTypeToForceRetBuf = false;
    if (unmanagedThisCallConv)
    {
        rewriteMetaSigFromExplicitThisToHasThis = true;
        // Also, any struct type other than a few special cases is returned via return buffer for unmanaged calls
        CorElementType retType = sig.GetReturnType();
        sig.Reset();

        if (retType == ELEMENT_TYPE_VALUETYPE)
        {
            TypeHandle thRetType = sig.GetRetTypeHandleThrowing();
            MethodTable* pMTRetType = thRetType.AsMethodTable();

            if (pMTRetType->GetInternalCorElementType() == ELEMENT_TYPE_VALUETYPE && !isNativePrimitiveStructType(pMTRetType))
            {
                rewriteReturnTypeToForceRetBuf = true;
            }
        }
    }
#endif // defined(TARGET_WINDOWS)

    // Rewrite ExplicitThis to HasThis. This allows us to use ArgIterator which is unaware of ExplicitThis
    // in the places where it is needed such as computation of return buffers.
    if (sig.GetCallingConventionInfo() & IMAGE_CEE_CS_CALLCONV_EXPLICITTHIS)
    {
        LOG2((LF2_INTERPRETER, LL_INFO10000, "Managed ExplicitThis to HasThis conversion needed\n"));
        rewriteMetaSigFromExplicitThisToHasThis = true;
    }

    SigBuilder sigBuilder;
    if (rewriteMetaSigFromExplicitThisToHasThis)
    {
        LOG2((LF2_INTERPRETER, LL_INFO10000, "Rewriting ExplicitThis to implicit this\n"));
        sigBuilder.AppendByte(IMAGE_CEE_CS_CALLCONV_DEFAULT_HASTHIS);
        if ((sig.NumFixedArgs() == 0) || (sig.HasThis() && !sig.HasExplicitThis()))
        {
            ThrowHR(COR_E_BADIMAGEFORMAT);
        }
        sigBuilder.AppendData(sig.NumFixedArgs() - 1);
        TypeHandle thRetType = sig.GetRetTypeHandleThrowing();
#if defined(TARGET_WINDOWS)
        if (rewriteReturnTypeToForceRetBuf)
        {
            // Change the return type to type large enough it will always need to be returned via return buffer
            thRetType = CoreLibBinder::GetClass(CLASS__STACKFRAMEITERATOR);
            _ASSERTE(thRetType.IsValueType());
            _ASSERTE(thRetType.GetSize() > 64);
            sigBuilder.AppendElementType(ELEMENT_TYPE_INTERNAL);
            sigBuilder.AppendPointer(thRetType.AsPtr());
        }
        else
#endif
        {
            SigPointer pReturn = sig.GetReturnProps();
            pReturn.ConvertToInternalExactlyOne(sig.GetModule(), sig.GetSigTypeContext(), &sigBuilder);
        }

        // Skip the explicit this argument
        sig.NextArg();

        // Copy rest of the arguments
        sig.NextArg();
        SigPointer pArgs = sig.GetArgProps();
        for (unsigned i = 1; i < sig.NumFixedArgs(); i++)
        {
            pArgs.ConvertToInternalExactlyOne(sig.GetModule(), sig.GetSigTypeContext(), &sigBuilder);
        }

        DWORD cSig;
        PCCOR_SIGNATURE pNewSig = (PCCOR_SIGNATURE)sigBuilder.GetSignature(&cSig);
        MetaSig newSig(pNewSig, cSig, sig.GetModule(), NULL, MetaSig::sigMember);
        sig = newSig;
    }

#if defined(TARGET_APPLE) && defined(TARGET_ARM64)
    CQuickArray<SwiftLoweringElement> swiftLoweringInfo;
    SigBuilder swiftSigBuilder;
    int swiftIndirectResultCount = 0;
    int swiftArgIndex = 0;

    m_hasSwiftReturnLowering = false;
    m_swiftReturnLowering = {};
    m_swiftSelfByRefSize = 0;

    if (isSwiftCallConv)
    {
        RewriteSignatureForSwiftLowering(sig, swiftSigBuilder, swiftLoweringInfo, swiftIndirectResultCount);
    }
#endif // TARGET_APPLE && TARGET_ARM64

    ArgIteratorType argIt(&sig);
    int32_t interpreterStackOffset = 0;

    m_currentRoutineType = RoutineType::None;
    m_r1 = NoRange; // indicates that there is no active range of general purpose registers
    m_r2 = 0;
    m_x1 = NoRange; // indicates that there is no active range of FP registers
    m_x2 = 0;
    m_s1 = NoRange; // indicates that there is no active range of stack arguments
    m_s2 = 0;
    m_routineIndex = 0;
    m_totalStackSize = argIt.SizeOfArgStack();
    LOG2((LF2_INTERPRETER, LL_INFO10000, "ComputeCallStub\n"));
    int numArgs = sig.NumFixedArgs() + (sig.HasThis() ? 1 : 0);

    if (argIt.HasThis())
    {
        LOG2((LF2_INTERPRETER, LL_INFO10000, "HasThis\n"));
        // The "this" argument register is not enumerated by the arg iterator, so
        // we need to "inject" it here.
        // CLR ABI specifies that unlike the native Windows x64 calling convention, it is passed in the first argument register.
        m_r1 = 0;
        m_currentRoutineType = RoutineType::GPReg;
        interpreterStackOffset += INTERP_STACK_SLOT_SIZE;
    }

    if (argIt.HasParamType())
    {
        LOG2((LF2_INTERPRETER, LL_INFO10000, "argIt.HasParamType\n"));
        // In the Interpreter calling convention the argument after the "this" pointer is the parameter type
        ArgLocDesc paramArgLocDesc;
        argIt.GetParamTypeLoc(&paramArgLocDesc);
        ProcessArgument<ArgIteratorType>(NULL, paramArgLocDesc, pRoutines);
        interpreterStackOffset += INTERP_STACK_SLOT_SIZE;
    }

    if (argIt.HasAsyncContinuation())
    {
        LOG2((LF2_INTERPRETER, LL_INFO10000, "argIt.HasAsyncContinuation\n"));
        // In the Interpreter calling convention the argument after the param type is the async continuation
        ArgLocDesc asyncContinuationLocDesc;
        argIt.GetAsyncContinuationLoc(&asyncContinuationLocDesc);
        ProcessArgument<ArgIteratorType>(NULL, asyncContinuationLocDesc, pRoutines);
        interpreterStackOffset += INTERP_STACK_SLOT_SIZE;
    }

#if defined(TARGET_APPLE) && defined(TARGET_ARM64)
    if (swiftIndirectResultCount > 0)
    {
#if LOG_COMPUTE_CALL_STUB
        LOG2((LF2_INTERPRETER, LL_INFO10000, "Emitting Load_SwiftIndirectResult routine\n"));
#endif
        TerminateCurrentRoutineIfNotOfNewType(RoutineType::SwiftIndirectResult, pRoutines);
        pRoutines[m_routineIndex++] = GetSwiftIndirectResultRoutine();
        m_currentRoutineType = RoutineType::None;
        interpreterStackOffset += INTERP_STACK_SLOT_SIZE;
    }
#endif

    int ofs;
    while ((ofs = argIt.GetNextOffset()) != TransitionBlock::InvalidOffset)
    {
        LOG2((LF2_INTERPRETER, LL_INFO10000, "Next argument\n"));
        ArgLocDesc argLocDesc;
        argIt.GetArgLoc(ofs, &argLocDesc);

        // Each argument takes at least one slot on the interpreter stack
        int interpStackSlotSize = INTERP_STACK_SLOT_SIZE;

        // Each entry on the interpreter stack is always aligned to at least 8 bytes, but some arguments are 16 byte aligned
        TypeHandle thArgTypeHandle;
        CorElementType corType = argIt.GetArgType(&thArgTypeHandle);
#if defined(TARGET_APPLE) && defined(TARGET_ARM64)
        if (isSwiftCallConv && m_interpreterToNative)
        {
            MethodTable* pArgMT = nullptr;

            if (corType == ELEMENT_TYPE_BYREF)
            {
                sig.GetByRefType(&thArgTypeHandle);
            }

            if (thArgTypeHandle.IsTypeDesc() && !thArgTypeHandle.AsTypeDesc()->GetTypeParam().IsNull())
            {
                pArgMT = thArgTypeHandle.AsTypeDesc()->GetTypeParam().AsMethodTable();
            }
            else if (!thArgTypeHandle.IsTypeDesc() && !thArgTypeHandle.IsNull())
            {
                pArgMT = thArgTypeHandle.AsMethodTable();
            }

            if (ProcessSwiftSpecialArgument(pArgMT, interpStackSlotSize, interpreterStackOffset, pRoutines))
            {
                swiftArgIndex++;
                continue;
            }
        }
#endif // TARGET_APPLE && TARGET_ARM64

        if ((corType == ELEMENT_TYPE_VALUETYPE) && thArgTypeHandle.GetSize() > INTERP_STACK_SLOT_SIZE)
        {
            unsigned align = std::clamp(CEEInfo::getClassAlignmentRequirementStatic(thArgTypeHandle), INTERP_STACK_SLOT_SIZE, INTERP_STACK_ALIGNMENT);
            assert(align == 8 || align == 16); // At the moment, we can only have an 8 or 16 byte alignment requirement here
            if (interpreterStackOffset != ALIGN_UP(interpreterStackOffset, align))
            {
                TerminateCurrentRoutineIfNotOfNewType(RoutineType::None, pRoutines);

                interpreterStackOffset += INTERP_STACK_SLOT_SIZE;
                pRoutines[m_routineIndex++] = (PCODE)InjectInterpStackAlign;
                LOG2((LF2_INTERPRETER, LL_INFO10000, "Inject stack align argument\n"));
            }

            assert(interpreterStackOffset == ALIGN_UP(interpreterStackOffset, align));

            interpStackSlotSize = ALIGN_UP(thArgTypeHandle.GetSize(), align);
        }

#if defined(TARGET_APPLE) && defined(TARGET_ARM64)
        if (isSwiftCallConv && m_interpreterToNative && swiftArgIndex < (int)swiftLoweringInfo.Size())
        {
            SwiftLoweringElement& elem = swiftLoweringInfo[swiftArgIndex];
            swiftArgIndex++;

            if (elem.isLowered)
            {
                if (elem.structSize != 0)
                {
                    interpreterStackOffset += elem.structSize;
                }
                EmitSwiftLoweredElementRoutine(elem, argLocDesc, pRoutines);
                continue;
            }
        }
#endif // TARGET_APPLE && TARGET_ARM64

        interpreterStackOffset += interpStackSlotSize;

#ifdef UNIX_AMD64_ABI
        ArgLocDesc* argLocDescForStructInRegs = argIt.GetArgLocDescForStructInRegs();
        if (argLocDescForStructInRegs != NULL)
        {
            int numEightBytes = argLocDescForStructInRegs->m_eightByteInfo.GetNumEightBytes();
            for (int i = 0; i < numEightBytes; i++)
            {
                ArgLocDesc argLocDescEightByte = {};
                SystemVClassificationType eightByteType = argLocDescForStructInRegs->m_eightByteInfo.GetEightByteClassification(i);
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
#elif defined(TARGET_ARM) && defined(ARM_SOFTFP)
        if (argLocDesc.m_cGenReg != 0 && argLocDesc.m_byteStackSize != 0)
        {
            ArgLocDesc argLocDescReg = {};
            argLocDescReg.m_idxGenReg = argLocDesc.m_idxGenReg;
            argLocDescReg.m_cGenReg = argLocDesc.m_cGenReg;
            ProcessArgument(&argIt, argLocDescReg, pRoutines);

            ArgLocDesc argLocDescStack = {};
            argLocDescStack.m_byteStackIndex = argLocDesc.m_byteStackIndex;
            argLocDescStack.m_byteStackSize = argLocDesc.m_byteStackSize;
            ProcessArgument(&argIt, argLocDescStack, pRoutines);
        }
        else
#endif // UNIX_AMD64_ABI
        {
            ProcessArgument(&argIt, argLocDesc, pRoutines);
        }
    }

    // All arguments were processed, but there is likely a pending ranges to store.
    // Process such a range if any.
    TerminateCurrentRoutineIfNotOfNewType(RoutineType::None, pRoutines);

    ReturnType returnType = GetReturnType(&argIt);

    if (m_interpreterToNative)
    {
        m_pInvokeFunction = GetInvokeFunctionPtr(returnType);
        m_targetSlotIndex = m_routineIndex;
        m_routineIndex++; // Reserve one extra slot for the target method pointer

#if defined(TARGET_APPLE) && defined(TARGET_ARM64)
        if (m_hasSwiftReturnLowering)
        {
            EmitSwiftReturnLoweringRoutines(pRoutines);
        }
#endif // TARGET_APPLE && TARGET_ARM64
    }
    else
    {
        pRoutines[m_routineIndex++] = GetInterpreterReturnTypeHandler(returnType);
    }
}

// Process the argument described by argLocDesc. This function is called for each argument in the method signature.
// It updates the ranges of registers and emits entries into the routines array at discontinuities.
template<typename ArgIteratorType>
void CallStubGenerator::ProcessArgument(ArgIteratorType *pArgIt, ArgLocDesc& argLocDesc, PCODE *pRoutines)
{
    LIMITED_METHOD_CONTRACT;

    RoutineType argType = RoutineType::None;
#ifdef TARGET_ARM
    if (argLocDesc.m_cGenReg == 2 || argLocDesc.m_byteStackSize >= 8)
    {
        /* do nothing */
    }
    else
#endif // TARGET_ARM
    if (argLocDesc.m_cGenReg != 0)
    {
        argType = RoutineType::GPReg;
    }
    else if (argLocDesc.m_cFloatReg != 0)
    {
#ifdef TARGET_ARM64
        if (argLocDesc.m_hfaFieldSize == 16)
        {
            argType = RoutineType::FPReg128;
        }
        else if (argLocDesc.m_hfaFieldSize == 4)
        {
            argType = RoutineType::FPReg32;
        }
        else
#endif // TARGET_ARM64
        {
            argType = RoutineType::FPReg;
        }
    }
    else if (argLocDesc.m_byteStackSize != 0)
    {
        argType = RoutineType::Stack;
    }

    TerminateCurrentRoutineIfNotOfNewType(argType, pRoutines);

    if (argLocDesc.m_cGenReg != 0)
    {
        LOG2((LF2_INTERPRETER, LL_INFO10000, "m_cGenReg=%d\n", (int)argLocDesc.m_cGenReg));
#ifdef TARGET_ARM
        if (argLocDesc.m_cGenReg == 2)
        {
            pRoutines[m_routineIndex++] = GetRegRoutine_4B(argLocDesc.m_idxGenReg, argLocDesc.m_idxGenReg + argLocDesc.m_cGenReg - 1);
        }
        else
#endif // TARGET_ARM
        if (m_r1 == NoRange) // No active range yet
        {
            // Start a new range
            m_r1 = argLocDesc.m_idxGenReg;
            m_r2 = m_r1 + argLocDesc.m_cGenReg - 1;
        }
        else if (argLocDesc.m_idxGenReg == m_r2 + 1
#ifdef ENREGISTERED_PARAMTYPE_MAXSIZE
                 && (!pArgIt || !pArgIt->IsArgPassedByRef())
#endif // ENREGISTERED_PARAMTYPE_MAXSIZE
                )
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
        LOG2((LF2_INTERPRETER, LL_INFO10000, "m_cFloatReg=%d\n", (int)argLocDesc.m_cFloatReg));
        if (m_x1 == NoRange) // No active range yet
        {
            // Start a new range
            m_x1 = argLocDesc.m_idxFloatReg;
            m_x2 = m_x1 + argLocDesc.m_cFloatReg - 1;
        }
        else if ((argLocDesc.m_idxFloatReg == m_x2 + 1) && (m_currentRoutineType == argType))
        {
            // Extend an existing range
            m_x2 += argLocDesc.m_cFloatReg;
        }
        else
        {
            // Discontinuous range - store a routine for the current and start a new one
            if (m_currentRoutineType == RoutineType::FPReg)
            {
                pRoutines[m_routineIndex++] = GetFPRegRangeRoutine(m_x1, m_x2);
            }
#ifdef TARGET_ARM64
            else if (m_currentRoutineType == RoutineType::FPReg32)
            {
                pRoutines[m_routineIndex++] = GetFPReg32RangeRoutine(m_x1, m_x2);
            }
            else if (m_currentRoutineType == RoutineType::FPReg128)
            {
                pRoutines[m_routineIndex++] = GetFPReg128RangeRoutine(m_x1, m_x2);
            }
#endif // TARGET_ARM64
            m_x1 = argLocDesc.m_idxFloatReg;
            m_x2 = m_x1 + argLocDesc.m_cFloatReg - 1;
        }

#ifdef TARGET_ARM64
        if ((argType == RoutineType::FPReg32) && ((argLocDesc.m_cFloatReg & 1) != 0))
        {
            // HFA Arguments using odd number of 32 bit FP registers cannot be merged with further ranges due to the
            // interpreter stack slot size alignment needs. The range copy routines for these registers
            // ensure that the interpreter stack is properly aligned after the odd number of registers are
            // loaded / stored.
            pRoutines[m_routineIndex++] = GetFPReg32RangeRoutine(m_x1, m_x2);
            argType = RoutineType::None;
            m_x1 = NoRange;
        }
#endif // TARGET_ARM64
    }

    if (argLocDesc.m_byteStackSize != 0)
    {
        LOG2((LF2_INTERPRETER, LL_INFO10000, "m_byteStackSize=%d\n", (int)argLocDesc.m_byteStackSize));
#ifdef TARGET_ARM
        if (argLocDesc.m_byteStackSize >= 8)
        {
            pRoutines[m_routineIndex++] = GetStackRoutine_4B();
            pRoutines[m_routineIndex++] = argLocDesc.m_byteStackIndex;
            pRoutines[m_routineIndex++] = argLocDesc.m_byteStackSize;
        }
        else
#endif // TARGET_ARM
        if (m_s1 == NoRange) // No active range yet
        {
            // Start a new range
            m_s1 = argLocDesc.m_byteStackIndex;
            m_s2 = m_s1 + argLocDesc.m_byteStackSize - 1;
        }
        else if ((argLocDesc.m_byteStackIndex == m_s2 + 1) && (argLocDesc.m_byteStackSize >= TARGET_POINTER_SIZE) && IS_ALIGNED(m_s2 - m_s1 + 1, INTERP_STACK_SLOT_SIZE)
#ifdef ENREGISTERED_PARAMTYPE_MAXSIZE
                 && (!pArgIt || !pArgIt->IsArgPassedByRef())
#endif // ENREGISTERED_PARAMTYPE_MAXSIZE
                )
        {
            // Extend an existing range, but only if the argument is at least pointer size large and
            // the existing range end was aligned to interpreter stack slot size.
            // The only case when this is not true is on Apple ARM64 OSes where primitive type smaller
            // than 8 bytes are passed on the stack in a packed manner. We process such arguments one by
            // one to avoid explosion of the number of pRoutines.
            m_s2 += argLocDesc.m_byteStackSize;
        }
        else
        {
            // Discontinuous range - store a routine for the current and start a new one
            pRoutines[m_routineIndex++] = GetStackRoutine();
#ifdef TARGET_32BIT
            pRoutines[m_routineIndex++] = m_s1;
            pRoutines[m_routineIndex++] = m_s2 - m_s1 + 1;
#else // !TARGET_32BIT
            pRoutines[m_routineIndex++] = ((int64_t)(m_s2 - m_s1 + 1) << 32) | m_s1;
#endif // TARGET_32BIT
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
            argType = RoutineType::None;
        }
#endif // TARGET_APPLE && TARGET_ARM64
    }

#ifndef UNIX_AMD64_ABI
    // Arguments passed by reference are handled separately, because the interpreter stores the value types on its stack by value.
    // So the argument loading routine needs to load the address of the argument. To avoid explosion of number of the routines,
    // we always process single argument passed by reference using single routine.
#ifdef ENREGISTERED_PARAMTYPE_MAXSIZE
    if (pArgIt != NULL && pArgIt->IsArgPassedByRef())
    {
        int unalignedArgSize = pArgIt->GetArgSize();
        // For the interpreter-to-native transition we need to make sure that we properly align the offsets
        //  to interpreter stack slots. Otherwise a VT of i.e. size 12 will misalign the stack offset during
        //  loads and we will start loading garbage into registers.
        // We don't need to do this for native-to-interpreter transitions because the Store_Ref_xxx helpers
        //  automatically do alignment of the stack offset themselves when updating the stack offset,
        //  and if we were to pass them aligned sizes they would potentially read bytes past the end of the VT.
        int alignedArgSize = m_interpreterToNative
            ? ALIGN_UP(unalignedArgSize, TARGET_POINTER_SIZE)
            : unalignedArgSize;

        if (argLocDesc.m_cGenReg == 1)
        {
            pRoutines[m_routineIndex++] = GetGPRegRefRoutine(argLocDesc.m_idxGenReg);
            pRoutines[m_routineIndex++] = alignedArgSize;
            m_r1 = NoRange;
            argType = RoutineType::None;
        }
        else
        {
            _ASSERTE(argLocDesc.m_byteStackIndex != -1);
            pRoutines[m_routineIndex++] = GetStackRefRoutine();
            pRoutines[m_routineIndex++] = ((int64_t)alignedArgSize << 32) | argLocDesc.m_byteStackIndex;
            m_s1 = NoRange;
            argType = RoutineType::None;
        }
    }
#endif // ENREGISTERED_PARAMTYPE_MAXSIZE
#endif // UNIX_AMD64_ABI

    m_currentRoutineType = argType;
}

template<typename ArgIteratorType>
CallStubGenerator::ReturnType CallStubGenerator::GetReturnType(ArgIteratorType *pArgIt)
{
#if defined(TARGET_APPLE) && defined(TARGET_ARM64)
    if (m_hasSwiftReturnLowering)
    {
        return ReturnTypeSwiftLowered;
    }
#endif // TARGET_APPLE && TARGET_ARM64

    if (pArgIt->HasRetBuffArg())
    {
#if defined(TARGET_AMD64) || defined(TARGET_ARM)
        if (pArgIt->HasThis())
        {
            return ReturnTypeBuffArg2;
        }
        else
        {
            return ReturnTypeBuffArg1;
        }
#else
#if defined(TARGET_WINDOWS) && defined(TARGET_ARM64)
        if (pArgIt->IsRetBuffPassedAsFirstArg())
        {
            _ASSERTE(pArgIt->HasThis());
            return ReturnTypeBuffArg2;
        }
#endif
        return ReturnTypeBuff;
#endif // TARGET_AMD64
    }
    else
    {
        TypeHandle thReturnValueType;
        CorElementType thReturnType = pArgIt->GetSig()->GetReturnTypeNormalized(&thReturnValueType);

        switch (thReturnType)
        {
            case ELEMENT_TYPE_I1:
                return ReturnTypeI1;
            case ELEMENT_TYPE_BOOLEAN:
            case ELEMENT_TYPE_U1:
                return ReturnTypeU1;
            case ELEMENT_TYPE_I2:
                return ReturnTypeI2;
            case ELEMENT_TYPE_CHAR:
            case ELEMENT_TYPE_U2:
                return ReturnTypeU2;
            case ELEMENT_TYPE_I4:
            case ELEMENT_TYPE_U4:
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
#ifdef TARGET_32BIT
                return ReturnTypeI4;
                break;
#endif // TARGET_32BIT
            case ELEMENT_TYPE_I8:
            case ELEMENT_TYPE_U8:
                return ReturnTypeI8;
                break;
            case ELEMENT_TYPE_R4:
#if defined(TARGET_ARM64) || defined(TARGET_32BIT)
                return ReturnTypeFloat;
#endif // TARGET_ARM64 || TARGET_32BIT
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
                if (!pArgIt->HasRetBuffArg())
                {
                    _ASSERTE(thReturnValueType.IsNativeValueType() ||  thReturnValueType.AsMethodTable()->IsRegPassedStruct());
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
                            case CORINFO_HFA_ELEM_VECTOR64:
                                switch (thReturnValueType.GetSize())
                                {
                                    case 8:
                                        return ReturnTypeVector64;
                                    case 16:
                                        return ReturnType2Vector64;
                                    case 24:
                                        return ReturnType3Vector64;
                                    case 32:
                                        return ReturnType4Vector64;
                                    default:
                                        _ASSERTE(!"Unsupported Vector64 HFA size");
                                        break;
                                }
                                break;
                            case CORINFO_HFA_ELEM_VECTOR128:
                                switch (thReturnValueType.GetSize())
                                {
                                    case 16:
                                        return ReturnTypeVector128;
                                    case 32:
                                        return ReturnType2Vector128;
                                    case 48:
                                        return ReturnType3Vector128;
                                    case 64:
                                        return ReturnType4Vector128;
                                    default:
                                        _ASSERTE(!"Unsupported Vector128 HFA size");
                                        break;
                                }
                                break;
                        default:
                            _ASSERTE(!"HFA type is not supported");
                            break;
                    }
                }
                else
                {
                    unsigned size = thReturnValueType.GetSize();
                    if (size <= 8)
                    {
                        return ReturnTypeI8;
                    }
                    else if (size <= 16)
                    {
                        return ReturnType2I8;
                    }
                    else
                    {
                        _ASSERTE(!"The return types that are not HFA should be <= 16 bytes in size");
                    }
                }
#elif TARGET_ARM
                switch (thReturnValueType.GetSize())
                {
                    case 1:
                    case 2:
                    case 4:
                        return ReturnTypeI4;
                        break;
                    case 8:
                        return ReturnTypeI8;
                    default:
                        _ASSERTE(!"The return types should be <= 8 bytes in size");
                        break;
                }
#elif defined(TARGET_RISCV64)
                {
                    FpStructInRegistersInfo info = pArgIt->GetReturnFpStructInRegistersInfo();
                    // RISC-V pass floating-point struct fields in FA registers
                    if ((info.flags & FpStruct::OnlyOne) != 0)
                    {
                        // Single field - could be float or int in single register
                        return ReturnTypeDouble; // Use Double routine for both float and double (NaN-boxed)
                    }
                    else if ((info.flags & FpStruct::BothFloat) != 0)
                    {
                        // Two float/double fields
                        return ReturnType2Double;
                    }
                    else if ((info.flags & FpStruct::FloatInt) != 0)
                    {
                        // First field float, second int
                        return ReturnTypeFloatInt;
                    }
                    else if ((info.flags & FpStruct::IntFloat) != 0)
                    {
                        // First field int, second float
                        return ReturnTypeIntFloat;
                    }
                    else
                    {
                        _ASSERTE(info.flags == FpStruct::UseIntCallConv);
                        _ASSERTE(thReturnValueType.AsMethodTable()->IsRegPassedStruct());
                        unsigned size = thReturnValueType.GetSize();
                        if (size <= 8)
                        {
                            return ReturnTypeI8;
                        }
                        else if (size <= 16)
                        {
                            return ReturnType2I8;
                        }
                        else
                        {
                            _ASSERTE(!"Struct returns should be <= 16 bytes in size");
                        }
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

#if defined(TARGET_APPLE) && defined(TARGET_ARM64)
void CallStubGenerator::RewriteSignatureForSwiftLowering(MetaSig &sig, SigBuilder &swiftSigBuilder, CQuickArray<SwiftLoweringElement> &swiftLoweringInfo, int &swiftIndirectResultCount)
{
    sig.Reset();
    TypeHandle thReturnType;
    CorElementType retCorType = sig.GetReturnTypeNormalized(&thReturnType);
    if (retCorType == ELEMENT_TYPE_VALUETYPE && !thReturnType.IsNull() && !thReturnType.IsTypeDesc())
    {
        MethodTable* pRetMT = thReturnType.AsMethodTable();
        if (pRetMT->IsValueType() && !pRetMT->IsHFA() &&
            !pRetMT->HasSameTypeDefAs(CoreLibBinder::GetClass(CLASS__VECTOR64T)) &&
            !pRetMT->HasSameTypeDefAs(CoreLibBinder::GetClass(CLASS__VECTOR128T)) &&
            !pRetMT->HasSameTypeDefAs(CoreLibBinder::GetClass(CLASS__VECTOR256T)) &&
            !pRetMT->HasSameTypeDefAs(CoreLibBinder::GetClass(CLASS__VECTOR512T)) &&
            !pRetMT->HasSameTypeDefAs(CoreLibBinder::GetClass(CLASS__VECTORT)))
        {
            CORINFO_SWIFT_LOWERING lowering = {};
            pRetMT->GetNativeSwiftPhysicalLowering(&lowering, false);
            if (!lowering.byReference && lowering.numLoweredElements > 0)
            {
                m_hasSwiftReturnLowering = true;
                m_swiftReturnLowering = lowering;
#if LOG_COMPUTE_CALL_STUB
                LOG2((LF2_INTERPRETER, LL_INFO10000, "Swift return lowering detected: %d elements\n", lowering.numLoweredElements));
#endif
            }
        }
    }

    // Count how many extra arguments we need due to Swift lowering
    sig.Reset();
    int newArgCount = 0;
    int swiftSelfCount = 0;
    int swiftErrorCount = 0;
    swiftIndirectResultCount = 0;
    CorElementType argType;
    while ((argType = sig.NextArg()) != ELEMENT_TYPE_END)
    {
        TypeHandle thArgType = sig.GetLastTypeHandleThrowing();
        MethodTable* pArgMT = nullptr;

        if (argType == ELEMENT_TYPE_BYREF)
        {
            sig.GetByRefType(&thArgType);
        }

        // Extract the underlying MT for pointer types or unwrapped byrefs
        if (thArgType.IsTypeDesc() && !thArgType.AsTypeDesc()->GetTypeParam().IsNull())
        {
            pArgMT = thArgType.AsTypeDesc()->GetTypeParam().AsMethodTable();
        }
        else if (!thArgType.IsTypeDesc() && !thArgType.IsNull())
        {
            pArgMT = thArgType.AsMethodTable();
        }

        if (pArgMT != nullptr)
        {
            if (!pArgMT->IsValueType())
            {
                COMPlusThrow(kInvalidProgramException);
            }

            if (pArgMT->HasSameTypeDefAs(CoreLibBinder::GetClass(CLASS__VECTOR64T)) ||
                pArgMT->HasSameTypeDefAs(CoreLibBinder::GetClass(CLASS__VECTOR128T)) ||
                pArgMT->HasSameTypeDefAs(CoreLibBinder::GetClass(CLASS__VECTOR256T)) ||
                pArgMT->HasSameTypeDefAs(CoreLibBinder::GetClass(CLASS__VECTOR512T)) ||
                pArgMT->HasSameTypeDefAs(CoreLibBinder::GetClass(CLASS__VECTORT)))
            {
                COMPlusThrow(kInvalidProgramException);
            }

            if (pArgMT == CoreLibBinder::GetClass(CLASS__SWIFT_SELF))
            {
                swiftSelfCount++;
                if (swiftSelfCount > 1)
                {
                    COMPlusThrow(kInvalidProgramException);
                }
                newArgCount++;
                continue;
            }

            if (pArgMT->HasSameTypeDefAs(CoreLibBinder::GetClass(CLASS__SWIFT_SELF_T)))
            {
                swiftSelfCount++;
                if (swiftSelfCount > 1)
                {
                    COMPlusThrow(kInvalidProgramException);
                }

                // Fall through for struct lowering
            }

            if (pArgMT == CoreLibBinder::GetClass(CLASS__SWIFT_ERROR))
            {
                swiftErrorCount++;
                m_hasSwiftError = true;
                if (swiftErrorCount > 1)
                {
                    COMPlusThrow(kInvalidProgramException);
                }
                newArgCount++;
                continue;
            }

            if (pArgMT == CoreLibBinder::GetClass(CLASS__SWIFT_INDIRECT_RESULT))
            {
                swiftIndirectResultCount++;
                if (swiftIndirectResultCount > 1)
                {
                    COMPlusThrow(kInvalidProgramException);
                }
                // SwiftIndirectResult goes in x8, not in argument registers
                continue;
            }

            if (argType == ELEMENT_TYPE_VALUETYPE)
            {
                CORINFO_SWIFT_LOWERING lowering = {};
                pArgMT->GetNativeSwiftPhysicalLowering(&lowering, false);

                if (!lowering.byReference && lowering.numLoweredElements > 0)
                {
                    newArgCount += (int)lowering.numLoweredElements;
                    continue;
                }
            }
        }

        newArgCount++;
    }

    if (!m_interpreterToNative)
    {
        sig.Reset();
        return;
    }

    swiftLoweringInfo.ReSizeThrows(newArgCount);
    int loweringIndex = 0;

    // Build new signature with lowered structs and store lowering info
    swiftSigBuilder.AppendByte((BYTE)sig.GetCallingConventionInfo());
    swiftSigBuilder.AppendData(newArgCount);

    // Copy return type
    SigPointer pReturn = sig.GetReturnProps();
    pReturn.ConvertToInternalExactlyOne(sig.GetModule(), sig.GetSigTypeContext(), &swiftSigBuilder);

    // Process arguments
    sig.Reset();
    while ((argType = sig.NextArg()) != ELEMENT_TYPE_END)
    {
       if (argType == ELEMENT_TYPE_VALUETYPE)
        {
            TypeHandle thArgType = sig.GetLastTypeHandleThrowing();
            MethodTable* pArgMT = thArgType.IsTypeDesc() ? nullptr : thArgType.AsMethodTable();
            if (pArgMT != nullptr)
            {
                if (pArgMT == CoreLibBinder::GetClass(CLASS__SWIFT_INDIRECT_RESULT))
                {
                    // SwiftIndirectResult goes in x8, not in argument registers
                    continue;
                }
                // Don't lower Swift* types except SwiftSelf<T>
                if (pArgMT == CoreLibBinder::GetClass(CLASS__SWIFT_SELF))
                {
                    SigPointer pArg = sig.GetArgProps();
                    pArg.ConvertToInternalExactlyOne(sig.GetModule(), sig.GetSigTypeContext(), &swiftSigBuilder);
                    swiftLoweringInfo[loweringIndex++] = { 0, 0, false, false };
                    continue;
                }

                CORINFO_SWIFT_LOWERING lowering = {};
                pArgMT->GetNativeSwiftPhysicalLowering(&lowering, false);

                if (!lowering.byReference && lowering.numLoweredElements > 0)
                {
                    // Emit primitive types instead of struct
                    int structSize = ALIGN_UP(pArgMT->GetNumInstanceFieldBytes(), INTERP_STACK_SLOT_SIZE);
                    for (size_t i = 0; i < lowering.numLoweredElements; i++)
                    {
                        bool isFloat = false;
                        switch (lowering.loweredElements[i])
                        {
                            case CORINFO_TYPE_BYTE:
                                swiftSigBuilder.AppendElementType(ELEMENT_TYPE_I1);
                                break;
                            case CORINFO_TYPE_UBYTE:
                                swiftSigBuilder.AppendElementType(ELEMENT_TYPE_U1);
                                break;
                            case CORINFO_TYPE_SHORT:
                                swiftSigBuilder.AppendElementType(ELEMENT_TYPE_I2);
                                break;
                            case CORINFO_TYPE_USHORT:
                                swiftSigBuilder.AppendElementType(ELEMENT_TYPE_U2);
                                break;
                            case CORINFO_TYPE_INT:
                                swiftSigBuilder.AppendElementType(ELEMENT_TYPE_I4);
                                break;
                            case CORINFO_TYPE_UINT:
                                swiftSigBuilder.AppendElementType(ELEMENT_TYPE_U4);
                                break;
                            case CORINFO_TYPE_LONG:
                                swiftSigBuilder.AppendElementType(ELEMENT_TYPE_I8);
                                break;
                            case CORINFO_TYPE_ULONG:
                                swiftSigBuilder.AppendElementType(ELEMENT_TYPE_U8);
                                break;
                            case CORINFO_TYPE_NATIVEINT:
                                swiftSigBuilder.AppendElementType(ELEMENT_TYPE_I);
                                break;
                            case CORINFO_TYPE_NATIVEUINT:
                                swiftSigBuilder.AppendElementType(ELEMENT_TYPE_U);
                                break;
                            case CORINFO_TYPE_FLOAT:
                                swiftSigBuilder.AppendElementType(ELEMENT_TYPE_R4);
                                isFloat = true;
                                break;
                            case CORINFO_TYPE_DOUBLE:
                                swiftSigBuilder.AppendElementType(ELEMENT_TYPE_R8);
                                isFloat = true;
                                break;
                            default:
                                swiftSigBuilder.AppendElementType(ELEMENT_TYPE_I);
                                break;
                        }
                        bool isLast = (i == lowering.numLoweredElements - 1);
                        swiftLoweringInfo[loweringIndex++] = {
                            (uint16_t)lowering.offsets[i],
                            isLast ? (uint16_t)structSize : (uint16_t)0,
                            isFloat,
                            true
                        };
                    }
                    continue;
                }
            }
        }

        SigPointer pArg = sig.GetArgProps();
        pArg.ConvertToInternalExactlyOne(sig.GetModule(), sig.GetSigTypeContext(), &swiftSigBuilder);
        swiftLoweringInfo[loweringIndex++] = { 0, 0, false, false };
    }

    DWORD cSwiftSig;
    PCCOR_SIGNATURE pSwiftSig = (PCCOR_SIGNATURE)swiftSigBuilder.GetSignature(&cSwiftSig);
    MetaSig swiftSig(pSwiftSig, cSwiftSig, sig.GetModule(), NULL, MetaSig::sigMember);
    sig = swiftSig;
}

bool CallStubGenerator::ProcessSwiftSpecialArgument(MethodTable* pArgMT, int interpStackSlotSize, int32_t &interpreterStackOffset, PCODE *pRoutines)
{
    if (pArgMT == nullptr)
    {
        return false;
    }

    if (pArgMT->HasSameTypeDefAs(CoreLibBinder::GetClass(CLASS__SWIFT_SELF_T)))
    {
        Instantiation inst = pArgMT->GetInstantiation();
        _ASSERTE(inst.GetNumArgs() != 0);
        TypeHandle innerType = inst[0];
        _ASSERTE(!innerType.IsNull() && !innerType.IsTypeDesc());
        MethodTable* pInnerMT = innerType.AsMethodTable();
#if DEBUG
        CORINFO_SWIFT_LOWERING lowering = {};
        pInnerMT->GetNativeSwiftPhysicalLowering(&lowering, false);
        _ASSERTE(lowering.byReference);
#endif // DEBUG

#if LOG_COMPUTE_CALL_STUB
        LOG2((LF2_INTERPRETER, LL_INFO10000, "SwiftSelf<T> argument detected\n"));
#endif
        TerminateCurrentRoutineIfNotOfNewType(RoutineType::SwiftSelfByRef, pRoutines);
        m_currentRoutineType = RoutineType::SwiftSelfByRef;

        int structSize = ALIGN_UP(pInnerMT->GetNumInstanceFieldBytes(), INTERP_STACK_SLOT_SIZE);
        m_swiftSelfByRefSize = structSize;
        interpreterStackOffset += structSize;
        return true;
    }

    if (pArgMT == CoreLibBinder::GetClass(CLASS__SWIFT_SELF))
    {
#if LOG_COMPUTE_CALL_STUB
        LOG2((LF2_INTERPRETER, LL_INFO10000, "Swift Self argument detected\n"));
#endif

        TerminateCurrentRoutineIfNotOfNewType(RoutineType::SwiftSelf, pRoutines);
        m_currentRoutineType = RoutineType::SwiftSelf;
        interpreterStackOffset += interpStackSlotSize;
        return true;
    }

    if (pArgMT == CoreLibBinder::GetClass(CLASS__SWIFT_ERROR))
    {
#if LOG_COMPUTE_CALL_STUB
        LOG2((LF2_INTERPRETER, LL_INFO10000, "Swift Error argument detected\n"));
#endif

        TerminateCurrentRoutineIfNotOfNewType(RoutineType::SwiftError, pRoutines);
        m_currentRoutineType = RoutineType::SwiftError;
        interpreterStackOffset += interpStackSlotSize;
        return true;
    }

    return false;
}

void CallStubGenerator::EmitSwiftLoweredElementRoutine(SwiftLoweringElement &elem, ArgLocDesc &argLocDesc, PCODE *pRoutines)
{
    TerminateCurrentRoutineIfNotOfNewType(RoutineType::None, pRoutines);

    if (elem.isFloat && argLocDesc.m_cFloatReg > 0)
    {
        int regIndex = argLocDesc.m_idxFloatReg;
        pRoutines[m_routineIndex++] = GetSwiftLoadFPAtOffsetRoutine(regIndex);
        // Pack offset (lower 16 bits) and structSize (bits 16-31)
        PCODE packedData = (PCODE)elem.offset | ((PCODE)elem.structSize << 16);
        pRoutines[m_routineIndex++] = packedData;
#if LOG_COMPUTE_CALL_STUB
        LOG2((LF2_INTERPRETER, LL_INFO10000, "Swift lowered element to FP reg: offset=%d, structSize=%d, reg=d%d\n",
               elem.offset, elem.structSize, regIndex));
#endif
    }
    else if (!elem.isFloat && argLocDesc.m_cGenReg > 0)
    {
        int regIndex = argLocDesc.m_idxGenReg;
        pRoutines[m_routineIndex++] = GetSwiftLoadGPAtOffsetRoutine(regIndex);
        // Pack offset (lower 16 bits) and structSize (bits 16-31)
        PCODE packedData = (PCODE)elem.offset | ((PCODE)elem.structSize << 16);
        pRoutines[m_routineIndex++] = packedData;
#if LOG_COMPUTE_CALL_STUB
        LOG2((LF2_INTERPRETER, LL_INFO10000, "Swift lowered element to GP reg: offset=%d, structSize=%d, reg=x%d\n",
               elem.offset, elem.structSize, regIndex));
#endif
    }
    else
    {
        // Spilled to stack
        pRoutines[m_routineIndex++] = (PCODE)Load_Stack_AtOffset;
        // Pack offset (lower 16 bits), structSize (bits 16-31), and stackOffset (bits 32-63)
        PCODE packedData = (PCODE)elem.offset |
                           ((PCODE)elem.structSize << 16) |
                           ((PCODE)argLocDesc.m_byteStackIndex << 32);
        pRoutines[m_routineIndex++] = packedData;
#if LOG_COMPUTE_CALL_STUB
        LOG2((LF2_INTERPRETER, LL_INFO10000, "Swift lowered element to stack: offset=%d, structSize=%d, stackOffset=%d\n",
               elem.offset, elem.structSize, argLocDesc.m_byteStackIndex));
#endif
    }
}

void CallStubGenerator::EmitSwiftReturnLoweringRoutines(PCODE *pRoutines)
{
    int gpRegIndex = 0;
    int fpRegIndex = 0;

    for (size_t i = 0; i < m_swiftReturnLowering.numLoweredElements; i++)
    {
        CorInfoType elemType = m_swiftReturnLowering.loweredElements[i];
        uint32_t offset = m_swiftReturnLowering.offsets[i];

        bool isFloat = (elemType == CORINFO_TYPE_FLOAT || elemType == CORINFO_TYPE_DOUBLE);

        if (isFloat)
        {
            _ASSERTE(fpRegIndex < 4);
            pRoutines[m_routineIndex++] = GetSwiftStoreFPAtOffsetRoutine(fpRegIndex);
            pRoutines[m_routineIndex++] = (PCODE)offset;
            fpRegIndex++;
#if LOG_COMPUTE_CALL_STUB
            LOG2((LF2_INTERPRETER, LL_INFO10000, "Swift return store FP d%d at offset %d\n", fpRegIndex - 1, offset));
#endif
        }
        else
        {
            _ASSERTE(gpRegIndex < 4);
            pRoutines[m_routineIndex++] = GetSwiftStoreGPAtOffsetRoutine(gpRegIndex);
            pRoutines[m_routineIndex++] = (PCODE)offset;
            gpRegIndex++;
#if LOG_COMPUTE_CALL_STUB
            LOG2((LF2_INTERPRETER, LL_INFO10000, "Swift return store GP x%d at offset %d\n", gpRegIndex - 1, offset));
#endif
        }
    }

    pRoutines[m_routineIndex++] = (PCODE)SwiftLoweredReturnTerminator;
}
#endif

#endif // FEATURE_INTERPRETER && !TARGET_WASM
