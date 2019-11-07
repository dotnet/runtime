// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//*****************************************************************************
// File: CordbRegisterSet.cpp
//

//
//*****************************************************************************
#include "primitives.h"

HRESULT CordbRegisterSet::GetRegistersAvailable(ULONG64 *pAvailable)
{
    FAIL_IF_NEUTERED(this);
    VALIDATE_POINTER_TO_OBJECT(pAvailable, ULONG64 *);

    *pAvailable = SETBITULONG64(REGISTER_INSTRUCTION_POINTER)
                | SETBITULONG64(REGISTER_STACK_POINTER)
                | SETBITULONG64(REGISTER_ARM_R0)
                | SETBITULONG64(REGISTER_ARM_R1)
                | SETBITULONG64(REGISTER_ARM_R2)
                | SETBITULONG64(REGISTER_ARM_R3)
                | SETBITULONG64(REGISTER_ARM_R4)
                | SETBITULONG64(REGISTER_ARM_R5)
                | SETBITULONG64(REGISTER_ARM_R6)
                | SETBITULONG64(REGISTER_ARM_R7)
                | SETBITULONG64(REGISTER_ARM_R8)
                | SETBITULONG64(REGISTER_ARM_R9)
                | SETBITULONG64(REGISTER_ARM_R10)
                | SETBITULONG64(REGISTER_ARM_R11)
                | SETBITULONG64(REGISTER_ARM_R12)
                | SETBITULONG64(REGISTER_ARM_LR);

    return S_OK;
}

HRESULT CordbRegisterSet::GetRegisters(ULONG64 mask, ULONG32 regCount, CORDB_REGISTER regBuffer[])
{
    PUBLIC_REENTRANT_API_ENTRY(this);
    FAIL_IF_NEUTERED(this);
    ATT_REQUIRE_STOPPED_MAY_FAIL(GetProcess());

    UINT iRegister = 0;

    VALIDATE_POINTER_TO_OBJECT_ARRAY(regBuffer, CORDB_REGISTER, regCount, true, true);

    // @ARMTODO: floating point support

    for (int i = REGISTER_INSTRUCTION_POINTER;
         i <= REGISTER_ARM_LR && iRegister < regCount;
         i++)
    {
        if (mask &  SETBITULONG64(i))
        {
            switch (i)
            {
            case REGISTER_INSTRUCTION_POINTER:
                regBuffer[iRegister++] = m_rd->PC; break;
            case REGISTER_STACK_POINTER:
                regBuffer[iRegister++] = m_rd->SP; break;
            case REGISTER_ARM_R0:
                regBuffer[iRegister++] = m_rd->R0; break;
            case REGISTER_ARM_R1:
                regBuffer[iRegister++] = m_rd->R1; break;
            case REGISTER_ARM_R2:
                regBuffer[iRegister++] = m_rd->R2; break;
            case REGISTER_ARM_R3:
                regBuffer[iRegister++] = m_rd->R3; break;
            case REGISTER_ARM_R4:
                regBuffer[iRegister++] = m_rd->R4; break;
            case REGISTER_ARM_R5:
                regBuffer[iRegister++] = m_rd->R5; break;
            case REGISTER_ARM_R6:
                regBuffer[iRegister++] = m_rd->R6; break;
            case REGISTER_ARM_R7:
                regBuffer[iRegister++] = m_rd->R7; break;
            case REGISTER_ARM_R8:
                regBuffer[iRegister++] = m_rd->R8; break;
            case REGISTER_ARM_R9:
                regBuffer[iRegister++] = m_rd->R9; break;
            case REGISTER_ARM_R10:
                regBuffer[iRegister++] = m_rd->R10; break;
            case REGISTER_ARM_R11:
                regBuffer[iRegister++] = m_rd->R11; break;
            case REGISTER_ARM_R12:
                regBuffer[iRegister++] = m_rd->R12; break;
            case REGISTER_ARM_LR:
                regBuffer[iRegister++] = m_rd->LR; break;
            }
        }
    }

    _ASSERTE (iRegister <= regCount);
    return S_OK;
}

HRESULT CordbRegisterSet::GetRegistersAvailable(ULONG32 regCount,
                                                BYTE    pAvailable[])
{
    PUBLIC_REENTRANT_API_ENTRY(this);
    FAIL_IF_NEUTERED(this);
    VALIDATE_POINTER_TO_OBJECT_ARRAY(pAvailable, CORDB_REGISTER, regCount, true, true);

    // Defer to adapter for v1.0 interface
    return GetRegistersAvailableAdapter(regCount, pAvailable);
}


HRESULT CordbRegisterSet::GetRegisters(ULONG32 maskCount, BYTE mask[],
                                       ULONG32 regCount, CORDB_REGISTER regBuffer[])
{
    PUBLIC_REENTRANT_API_ENTRY(this);
    FAIL_IF_NEUTERED(this);
    VALIDATE_POINTER_TO_OBJECT_ARRAY(regBuffer, CORDB_REGISTER, regCount, true, true);

    // Defer to adapter for v1.0 interface
    return GetRegistersAdapter(maskCount, mask, regCount, regBuffer);
}

// This is just a convenience function to convert a regdisplay into a Context.
// Since a context has more info than a regdisplay, the conversion isn't perfect
// and the context can't be fully accurate.
void CordbRegisterSet::InternalCopyRDToContext(DT_CONTEXT * pInputContext)
{
    INTERNAL_SYNC_API_ENTRY(GetProcess());
    _ASSERTE(pInputContext);

    if ((pInputContext->ContextFlags & DT_CONTEXT_INTEGER) == DT_CONTEXT_INTEGER)
    {
        pInputContext->R0 = m_rd->R0;
        pInputContext->R1 = m_rd->R1;
        pInputContext->R2 = m_rd->R2;
        pInputContext->R3 = m_rd->R3;
        pInputContext->R4 = m_rd->R4;
        pInputContext->R5 = m_rd->R5;
        pInputContext->R6 = m_rd->R6;
        pInputContext->R7 = m_rd->R7;
        pInputContext->R8 = m_rd->R8;
        pInputContext->R9 = m_rd->R9;
        pInputContext->R10 = m_rd->R10;
        pInputContext->R11 = m_rd->R11;
    }

    if ((pInputContext->ContextFlags & DT_CONTEXT_CONTROL) == DT_CONTEXT_CONTROL)
    {
        pInputContext->Sp = m_rd->SP;
        pInputContext->Lr = m_rd->LR;
        pInputContext->Pc = m_rd->PC;
    }
}
