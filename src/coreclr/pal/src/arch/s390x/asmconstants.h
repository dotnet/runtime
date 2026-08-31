// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef __PAL_S390X_ASMCONSTANTS_H__
#define __PAL_S390X_ASMCONSTANTS_H__

#define CONTEXT_S390X   0x100000

#define CONTEXT_CONTROL 1 // PSW and R15
#define CONTEXT_INTEGER 2 // R0-R14
#define CONTEXT_FLOATING_POINT 4 // F0-F15

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
#define CONTEXT_F0            CONTEXT_R15+8
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
#define CONTEXT_PSWMask       CONTEXT_F15+8
#define CONTEXT_PSWAddr       CONTEXT_PSWMask+8
#define CONTEXT_Size          CONTEXT_PSWAddr+8

#endif
