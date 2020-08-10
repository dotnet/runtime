// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef __PAL_ARM_ASMCONSTANTS_H__
#define __PAL_ARM_ASMCONSTANTS_H__

#define CONTEXT_ContextFlags 0
#define CONTEXT_R0           CONTEXT_ContextFlags+4
#define CONTEXT_R1           CONTEXT_R0+4
#define CONTEXT_R2           CONTEXT_R1+4
#define CONTEXT_R3           CONTEXT_R2+4
#define CONTEXT_R4           CONTEXT_R3+4
#define CONTEXT_R5           CONTEXT_R4+4
#define CONTEXT_R6           CONTEXT_R5+4
#define CONTEXT_R7           CONTEXT_R6+4
#define CONTEXT_R8           CONTEXT_R7+4
#define CONTEXT_R9           CONTEXT_R8+4
#define CONTEXT_R10          CONTEXT_R9+4
#define CONTEXT_R11          CONTEXT_R10+4
#define CONTEXT_R12          CONTEXT_R11+4
#define CONTEXT_Sp           CONTEXT_R12+4
#define CONTEXT_Lr           CONTEXT_Sp+4
#define CONTEXT_Pc           CONTEXT_Lr+4
#define CONTEXT_Cpsr         CONTEXT_Pc+4
#define CONTEXT_Fpscr        CONTEXT_Cpsr+4
#define CONTEXT_Padding      CONTEXT_Fpscr+4
#define CONTEXT_D0           CONTEXT_Padding+4
#define CONTEXT_D1           CONTEXT_D0+8
#define CONTEXT_D2           CONTEXT_D1+8
#define CONTEXT_D3           CONTEXT_D2+8
#define CONTEXT_D4           CONTEXT_D3+8
#define CONTEXT_D5           CONTEXT_D4+8
#define CONTEXT_D6           CONTEXT_D5+8
#define CONTEXT_D7           CONTEXT_D6+8
#define CONTEXT_D8           CONTEXT_D7+8
#define CONTEXT_D9           CONTEXT_D8+8
#define CONTEXT_D10          CONTEXT_D9+8
#define CONTEXT_D11          CONTEXT_D10+8
#define CONTEXT_D12          CONTEXT_D11+8
#define CONTEXT_D13          CONTEXT_D12+8
#define CONTEXT_D14          CONTEXT_D13+8
#define CONTEXT_D15          CONTEXT_D14+8
#define CONTEXT_D16          CONTEXT_D15+8
#define CONTEXT_D17          CONTEXT_D16+8
#define CONTEXT_D18          CONTEXT_D17+8
#define CONTEXT_D19          CONTEXT_D18+8
#define CONTEXT_D20          CONTEXT_D19+8
#define CONTEXT_D21          CONTEXT_D20+8
#define CONTEXT_D22          CONTEXT_D21+8
#define CONTEXT_D23          CONTEXT_D22+8
#define CONTEXT_D24          CONTEXT_D23+8
#define CONTEXT_D25          CONTEXT_D24+8
#define CONTEXT_D26          CONTEXT_D25+8
#define CONTEXT_D27          CONTEXT_D26+8
#define CONTEXT_D28          CONTEXT_D27+8
#define CONTEXT_D29          CONTEXT_D28+8
#define CONTEXT_D30          CONTEXT_D29+8
#define CONTEXT_D31          CONTEXT_D30+8

#endif
