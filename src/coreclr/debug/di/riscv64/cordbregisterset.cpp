// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//*****************************************************************************
// File: CordbRegisterSet.cpp
//

//
//*****************************************************************************
#include "primitives.h"


HRESULT CordbRegisterSet::GetRegistersAvailable(ULONG64* pAvailable)
{
    FAIL_IF_NEUTERED(this);
    VALIDATE_POINTER_TO_OBJECT(pAvailable, ULONG64 *);

    *pAvailable = SETBITULONG64(REGISTER_RISCV64_PC)
                | SETBITULONG64(REGISTER_RISCV64_RA)
                | SETBITULONG64(REGISTER_RISCV64_SP)
                | SETBITULONG64(REGISTER_RISCV64_GP)
                | SETBITULONG64(REGISTER_RISCV64_TP)
                | SETBITULONG64(REGISTER_RISCV64_T0)
                | SETBITULONG64(REGISTER_RISCV64_T1)
                | SETBITULONG64(REGISTER_RISCV64_T2)
                | SETBITULONG64(REGISTER_RISCV64_FP)
                | SETBITULONG64(REGISTER_RISCV64_S1)
                | SETBITULONG64(REGISTER_RISCV64_A0)
                | SETBITULONG64(REGISTER_RISCV64_A1)
                | SETBITULONG64(REGISTER_RISCV64_A2)
                | SETBITULONG64(REGISTER_RISCV64_A3)
                | SETBITULONG64(REGISTER_RISCV64_A4)
                | SETBITULONG64(REGISTER_RISCV64_A5)
                | SETBITULONG64(REGISTER_RISCV64_A6)
                | SETBITULONG64(REGISTER_RISCV64_A7)
                | SETBITULONG64(REGISTER_RISCV64_S2)
                | SETBITULONG64(REGISTER_RISCV64_S3)
                | SETBITULONG64(REGISTER_RISCV64_S4)
                | SETBITULONG64(REGISTER_RISCV64_S5)
                | SETBITULONG64(REGISTER_RISCV64_S6)
                | SETBITULONG64(REGISTER_RISCV64_S7)
                | SETBITULONG64(REGISTER_RISCV64_S8)
                | SETBITULONG64(REGISTER_RISCV64_S9)
                | SETBITULONG64(REGISTER_RISCV64_S10)
                | SETBITULONG64(REGISTER_RISCV64_S11)
                | SETBITULONG64(REGISTER_RISCV64_T3)
                | SETBITULONG64(REGISTER_RISCV64_T4)
                | SETBITULONG64(REGISTER_RISCV64_T5)
                | SETBITULONG64(REGISTER_RISCV64_T6)
                | SETBITULONG64(REGISTER_RISCV64_F0)
                | SETBITULONG64(REGISTER_RISCV64_F1)
                | SETBITULONG64(REGISTER_RISCV64_F2)
                | SETBITULONG64(REGISTER_RISCV64_F3)
                | SETBITULONG64(REGISTER_RISCV64_F4)
                | SETBITULONG64(REGISTER_RISCV64_F5)
                | SETBITULONG64(REGISTER_RISCV64_F6)
                | SETBITULONG64(REGISTER_RISCV64_F7)
                | SETBITULONG64(REGISTER_RISCV64_F8)
                | SETBITULONG64(REGISTER_RISCV64_F9)
                | SETBITULONG64(REGISTER_RISCV64_F10)
                | SETBITULONG64(REGISTER_RISCV64_F11)
                | SETBITULONG64(REGISTER_RISCV64_F12)
                | SETBITULONG64(REGISTER_RISCV64_F13)
                | SETBITULONG64(REGISTER_RISCV64_F14)
                | SETBITULONG64(REGISTER_RISCV64_F15)
                | SETBITULONG64(REGISTER_RISCV64_F16)
                | SETBITULONG64(REGISTER_RISCV64_F17)
                | SETBITULONG64(REGISTER_RISCV64_F18)
                | SETBITULONG64(REGISTER_RISCV64_F19)
                | SETBITULONG64(REGISTER_RISCV64_F20)
                | SETBITULONG64(REGISTER_RISCV64_F21)
                | SETBITULONG64(REGISTER_RISCV64_F22)
                | SETBITULONG64(REGISTER_RISCV64_F23)
                | SETBITULONG64(REGISTER_RISCV64_F24)
                | SETBITULONG64(REGISTER_RISCV64_F25)
                | SETBITULONG64(REGISTER_RISCV64_F26)
                | SETBITULONG64(REGISTER_RISCV64_F27)
                | SETBITULONG64(REGISTER_RISCV64_F28)
                | SETBITULONG64(REGISTER_RISCV64_F29)
                | SETBITULONG64(REGISTER_RISCV64_F30)
                | SETBITULONG64(REGISTER_RISCV64_F31);

    return S_OK;
}

HRESULT CordbRegisterSet::GetRegisters(ULONG64 mask, ULONG32 regCount,
                                       CORDB_REGISTER regBuffer[])
{
    _ASSERTE(!"RISCV64:NYI");
    return S_OK;
}


HRESULT CordbRegisterSet::GetRegistersAvailable(ULONG32 regCount,
                                                BYTE    pAvailable[])
{
    _ASSERTE(!"RISCV64:NYI");
    return S_OK;
}


HRESULT CordbRegisterSet::GetRegisters(ULONG32 maskCount, BYTE mask[],
                                       ULONG32 regCount, CORDB_REGISTER regBuffer[])
{
    _ASSERTE(!"RISCV64:NYI");
    return S_OK;
}


// This is just a convenience function to convert a regdisplay into a Context.
// Since a context has more info than a regdisplay, the conversion isn't perfect
// and the context can't be fully accurate.
void CordbRegisterSet::InternalCopyRDToContext(DT_CONTEXT *pInputContext)
{
    _ASSERTE(!"RISCV64:NYI");
}
