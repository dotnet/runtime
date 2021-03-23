// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#define CONTEXT_ContextFlags 0
#define CONTEXT_FLOATING_POINT 8
#define CONTEXT_FloatSave 7*4
#define FLOATING_SAVE_AREA_SIZE 8*4+80
#define CONTEXT_Edi CONTEXT_FloatSave + FLOATING_SAVE_AREA_SIZE + 4*4
#define CONTEXT_Esi CONTEXT_Edi+4
#define CONTEXT_Ebx CONTEXT_Esi+4
#define CONTEXT_Edx CONTEXT_Ebx+4
#define CONTEXT_Ecx CONTEXT_Edx+4
#define CONTEXT_Eax CONTEXT_Ecx+4
#define CONTEXT_Ebp CONTEXT_Eax+4
#define CONTEXT_Eip CONTEXT_Ebp+4
#define CONTEXT_SegCs CONTEXT_Eip+4
#define CONTEXT_EFlags CONTEXT_SegCs+4
#define CONTEXT_Esp CONTEXT_EFlags+4
#define CONTEXT_SegSs CONTEXT_Esp+4
#define CONTEXT_EXTENDED_REGISTERS 32
#define CONTEXT_ExtendedRegisters CONTEXT_SegSs+4
#define CONTEXT_Xmm0 CONTEXT_ExtendedRegisters+160
#define CONTEXT_Xmm1 CONTEXT_Xmm0+16
#define CONTEXT_Xmm2 CONTEXT_Xmm1+16
#define CONTEXT_Xmm3 CONTEXT_Xmm2+16
#define CONTEXT_Xmm4 CONTEXT_Xmm3+16
#define CONTEXT_Xmm5 CONTEXT_Xmm4+16
#define CONTEXT_Xmm6 CONTEXT_Xmm5+16
#define CONTEXT_Xmm7 CONTEXT_Xmm6+16
