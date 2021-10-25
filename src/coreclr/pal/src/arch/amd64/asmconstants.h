// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifdef HOST_64BIT

// The arch bit is normally set in the flag constants below. Since this is already arch-specific code and the arch bit is not
// relevant, the arch bit is excluded from the flag constants below for simpler tests.
#define CONTEXT_AMD64   0x100000

#define CONTEXT_CONTROL 1 // SegSs, Rsp, SegCs, Rip, and EFlags
#define CONTEXT_INTEGER 2 // Rax, Rcx, Rdx, Rbx, Rbp, Rsi, Rdi, R8-R15
#define CONTEXT_SEGMENTS 4 // SegDs, SegEs, SegFs, SegGs
#define CONTEXT_FLOATING_POINT 8
#define CONTEXT_DEBUG_REGISTERS 16 // Dr0-Dr3 and Dr6-Dr7

#define CONTEXT_FULL (CONTEXT_CONTROL | CONTEXT_INTEGER | CONTEXT_FLOATING_POINT)

#define CONTEXT_XSTATE 64

#define CONTEXT_ContextFlags 6*8
#define CONTEXT_SegCs CONTEXT_ContextFlags+8
#define CONTEXT_SegDs CONTEXT_SegCs+2
#define CONTEXT_SegEs CONTEXT_SegDs+2
#define CONTEXT_SegFs CONTEXT_SegEs+2
#define CONTEXT_SegGs CONTEXT_SegFs+2
#define CONTEXT_SegSs CONTEXT_SegGs+2
#define CONTEXT_EFlags CONTEXT_SegSs+2
#define CONTEXT_Dr0 CONTEXT_EFlags+4
#define CONTEXT_Dr1 CONTEXT_Dr0+8
#define CONTEXT_Dr2 CONTEXT_Dr1+8
#define CONTEXT_Dr3 CONTEXT_Dr2+8
#define CONTEXT_Dr6 CONTEXT_Dr3+8
#define CONTEXT_Dr7 CONTEXT_Dr6+8
#define CONTEXT_Rax CONTEXT_Dr7+8
#define CONTEXT_Rcx CONTEXT_Rax+8
#define CONTEXT_Rdx CONTEXT_Rcx+8
#define CONTEXT_Rbx CONTEXT_Rdx+8
#define CONTEXT_Rsp CONTEXT_Rbx+8
#define CONTEXT_Rbp CONTEXT_Rsp+8
#define CONTEXT_Rsi CONTEXT_Rbp+8
#define CONTEXT_Rdi CONTEXT_Rsi+8
#define CONTEXT_R8 CONTEXT_Rdi+8
#define CONTEXT_R9 CONTEXT_R8+8
#define CONTEXT_R10 CONTEXT_R9+8
#define CONTEXT_R11 CONTEXT_R10+8
#define CONTEXT_R12 CONTEXT_R11+8
#define CONTEXT_R13 CONTEXT_R12+8
#define CONTEXT_R14 CONTEXT_R13+8
#define CONTEXT_R15 CONTEXT_R14+8
#define CONTEXT_Rip CONTEXT_R15+8
#define CONTEXT_FltSave CONTEXT_Rip+8
#define FLOATING_SAVE_AREA_SIZE 4*8+24*16+96
#define CONTEXT_Xmm0 CONTEXT_FltSave+10*16
#define CONTEXT_Xmm1 CONTEXT_Xmm0+16
#define CONTEXT_Xmm2 CONTEXT_Xmm1+16
#define CONTEXT_Xmm3 CONTEXT_Xmm2+16
#define CONTEXT_Xmm4 CONTEXT_Xmm3+16
#define CONTEXT_Xmm5 CONTEXT_Xmm4+16
#define CONTEXT_Xmm6 CONTEXT_Xmm5+16
#define CONTEXT_Xmm7 CONTEXT_Xmm6+16
#define CONTEXT_Xmm8 CONTEXT_Xmm7+16
#define CONTEXT_Xmm9 CONTEXT_Xmm8+16
#define CONTEXT_Xmm10 CONTEXT_Xmm9+16
#define CONTEXT_Xmm11 CONTEXT_Xmm10+16
#define CONTEXT_Xmm12 CONTEXT_Xmm11+16
#define CONTEXT_Xmm13 CONTEXT_Xmm12+16
#define CONTEXT_Xmm14 CONTEXT_Xmm13+16
#define CONTEXT_Xmm15 CONTEXT_Xmm14+16
#define CONTEXT_VectorRegister CONTEXT_FltSave+FLOATING_SAVE_AREA_SIZE
#define CONTEXT_VectorControl CONTEXT_VectorRegister+16*26
#define CONTEXT_DebugControl CONTEXT_VectorControl+8
#define CONTEXT_LastBranchToRip CONTEXT_DebugControl+8
#define CONTEXT_LastBranchFromRip CONTEXT_LastBranchToRip+8
#define CONTEXT_LastExceptionToRip CONTEXT_LastBranchFromRip+8
#define CONTEXT_LastExceptionFromRip CONTEXT_LastExceptionToRip+8
#define CONTEXT_Size CONTEXT_LastExceptionFromRip+8

#else // HOST_64BIT

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

#endif // HOST_64BIT
