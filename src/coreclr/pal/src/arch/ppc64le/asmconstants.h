// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef __PAL_POWERPC_ASMCONSTANTS_H__
#define __PAL_POWERPC_ASMCONSTANTS_H__

#define CONTEXT_PPC64   0x100000

#define CONTEXT_CONTROL 1 
#define CONTEXT_INTEGER 2 
#define CONTEXT_FLOATING_POINT 4 

#define CONTEXT_FULL (CONTEXT_CONTROL | CONTEXT_INTEGER | CONTEXT_FLOATING_POINT)

#define CONTEXT_ContextFlags  0
#define CONTEXT_R0            CONTEXT_ContextFlags+8
#define CONTEXT_R1            CONTEXT_R0+8
#define CONTEXT_R2            CONTEXT_R1+8
#define CONTEXT_R3            CONTEXT_R2+8
#define CONTEXT_R4            CONTEXT_R3+8
#define CONTEXT_R5            CONTEXT_R4+8
#define CONTEXT_R6            CONTEXT_R5+8
#define CONTEXT_R7            CONTEXT_R6+8
#define CONTEXT_R8            CONTEXT_R7+8
#define CONTEXT_R9            CONTEXT_R8+8
#define CONTEXT_R10           CONTEXT_R9+8
#define CONTEXT_R11           CONTEXT_R10+8
#define CONTEXT_R12           CONTEXT_R11+8
#define CONTEXT_R13           CONTEXT_R12+8
#define CONTEXT_R14           CONTEXT_R13+8
#define CONTEXT_R15           CONTEXT_R14+8
#define CONTEXT_R16           CONTEXT_R15+8
#define CONTEXT_R17           CONTEXT_R16+8
#define CONTEXT_R18           CONTEXT_R17+8
#define CONTEXT_R19           CONTEXT_R18+8
#define CONTEXT_R20           CONTEXT_R19+8
#define CONTEXT_R21           CONTEXT_R20+8
#define CONTEXT_R22           CONTEXT_R21+8
#define CONTEXT_R23           CONTEXT_R22+8
#define CONTEXT_R24           CONTEXT_R23+8
#define CONTEXT_R25           CONTEXT_R24+8
#define CONTEXT_R26           CONTEXT_R25+8
#define CONTEXT_R27           CONTEXT_R26+8
#define CONTEXT_R28           CONTEXT_R27+8
#define CONTEXT_R29           CONTEXT_R28+8
#define CONTEXT_R30           CONTEXT_R29+8
#define CONTEXT_R31           CONTEXT_R30+8
#define CONTEXT_F0            CONTEXT_R31+8
#define CONTEXT_F1            CONTEXT_F0+8
#define CONTEXT_F2            CONTEXT_F1+8
#define CONTEXT_F3            CONTEXT_F2+8
#define CONTEXT_F4            CONTEXT_F3+8
#define CONTEXT_F5            CONTEXT_F4+8
#define CONTEXT_F6            CONTEXT_F5+8
#define CONTEXT_F7            CONTEXT_F6+8
#define CONTEXT_F8            CONTEXT_F7+8
#define CONTEXT_F9            CONTEXT_F8+8
#define CONTEXT_F10           CONTEXT_F9+8
#define CONTEXT_F11           CONTEXT_F10+8
#define CONTEXT_F12           CONTEXT_F11+8
#define CONTEXT_F13           CONTEXT_F12+8
#define CONTEXT_F14           CONTEXT_F13+8
#define CONTEXT_F15           CONTEXT_F14+8
#define CONTEXT_F16           CONTEXT_F15+8
#define CONTEXT_F17           CONTEXT_F16+8
#define CONTEXT_F18           CONTEXT_F17+8
#define CONTEXT_F19           CONTEXT_F18+8
#define CONTEXT_F20           CONTEXT_F19+8
#define CONTEXT_F21           CONTEXT_F20+8
#define CONTEXT_F22           CONTEXT_F21+8
#define CONTEXT_F23           CONTEXT_F22+8
#define CONTEXT_F24           CONTEXT_F23+8
#define CONTEXT_F25           CONTEXT_F24+8
#define CONTEXT_F26           CONTEXT_F25+8
#define CONTEXT_F27           CONTEXT_F26+8
#define CONTEXT_F28           CONTEXT_F27+8
#define CONTEXT_F29           CONTEXT_F28+8
#define CONTEXT_F30           CONTEXT_F29+8
#define CONTEXT_F31           CONTEXT_F30+8
#define CONTEXT_FPSCR         CONTEXT_F31+8
#define CONTEXT_NIP           CONTEXT_FPSCR+8
#define CONTEXT_MSR           CONTEXT_NIP+8
#define CONTEXT_CTR           CONTEXT_MSR+8
#define CONTEXT_LINK          CONTEXT_CTR+8
#define CONTEXT_XER           CONTEXT_LINK+8
#define CONTEXT_CCR           CONTEXT_XER+8
#define CONTEXT_Size          CONTEXT_CCR+8

#endif
