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

    *pAvailable = SETBITULONG64(REGISTER_LOONGARCH64_PC)
                | SETBITULONG64(REGISTER_LOONGARCH64_SP)
                | SETBITULONG64(REGISTER_LOONGARCH64_FP)
                | SETBITULONG64(REGISTER_LOONGARCH64_RA)
                | SETBITULONG64(REGISTER_LOONGARCH64_TP)
                | SETBITULONG64(REGISTER_LOONGARCH64_A0)
                | SETBITULONG64(REGISTER_LOONGARCH64_A1)
                | SETBITULONG64(REGISTER_LOONGARCH64_A2)
                | SETBITULONG64(REGISTER_LOONGARCH64_A3)
                | SETBITULONG64(REGISTER_LOONGARCH64_A4)
                | SETBITULONG64(REGISTER_LOONGARCH64_A5)
                | SETBITULONG64(REGISTER_LOONGARCH64_A6)
                | SETBITULONG64(REGISTER_LOONGARCH64_A7)
                | SETBITULONG64(REGISTER_LOONGARCH64_T0)
                | SETBITULONG64(REGISTER_LOONGARCH64_T1)
                | SETBITULONG64(REGISTER_LOONGARCH64_T2)
                | SETBITULONG64(REGISTER_LOONGARCH64_T3)
                | SETBITULONG64(REGISTER_LOONGARCH64_T4)
                | SETBITULONG64(REGISTER_LOONGARCH64_T5)
                | SETBITULONG64(REGISTER_LOONGARCH64_T6)
                | SETBITULONG64(REGISTER_LOONGARCH64_T7)
                | SETBITULONG64(REGISTER_LOONGARCH64_T8)
                | SETBITULONG64(REGISTER_LOONGARCH64_X0)
                | SETBITULONG64(REGISTER_LOONGARCH64_S0)
                | SETBITULONG64(REGISTER_LOONGARCH64_S1)
                | SETBITULONG64(REGISTER_LOONGARCH64_S2)
                | SETBITULONG64(REGISTER_LOONGARCH64_S3)
                | SETBITULONG64(REGISTER_LOONGARCH64_S4)
                | SETBITULONG64(REGISTER_LOONGARCH64_S5)
                | SETBITULONG64(REGISTER_LOONGARCH64_S6)
                | SETBITULONG64(REGISTER_LOONGARCH64_S7)
                | SETBITULONG64(REGISTER_LOONGARCH64_S8)
                | SETBITULONG64(REGISTER_LOONGARCH64_F0)
                | SETBITULONG64(REGISTER_LOONGARCH64_F1)
                | SETBITULONG64(REGISTER_LOONGARCH64_F2)
                | SETBITULONG64(REGISTER_LOONGARCH64_F3)
                | SETBITULONG64(REGISTER_LOONGARCH64_F4)
                | SETBITULONG64(REGISTER_LOONGARCH64_F5)
                | SETBITULONG64(REGISTER_LOONGARCH64_F6)
                | SETBITULONG64(REGISTER_LOONGARCH64_F7)
                | SETBITULONG64(REGISTER_LOONGARCH64_F8)
                | SETBITULONG64(REGISTER_LOONGARCH64_F9)
                | SETBITULONG64(REGISTER_LOONGARCH64_F10)
                | SETBITULONG64(REGISTER_LOONGARCH64_F11)
                | SETBITULONG64(REGISTER_LOONGARCH64_F12)
                | SETBITULONG64(REGISTER_LOONGARCH64_F13)
                | SETBITULONG64(REGISTER_LOONGARCH64_F14)
                | SETBITULONG64(REGISTER_LOONGARCH64_F15)
                | SETBITULONG64(REGISTER_LOONGARCH64_F16)
                | SETBITULONG64(REGISTER_LOONGARCH64_F17)
                | SETBITULONG64(REGISTER_LOONGARCH64_F18)
                | SETBITULONG64(REGISTER_LOONGARCH64_F19)
                | SETBITULONG64(REGISTER_LOONGARCH64_F20)
                | SETBITULONG64(REGISTER_LOONGARCH64_F21)
                | SETBITULONG64(REGISTER_LOONGARCH64_F22)
                | SETBITULONG64(REGISTER_LOONGARCH64_F23)
                | SETBITULONG64(REGISTER_LOONGARCH64_F24)
                | SETBITULONG64(REGISTER_LOONGARCH64_F25)
                | SETBITULONG64(REGISTER_LOONGARCH64_F26)
                | SETBITULONG64(REGISTER_LOONGARCH64_F27)
                | SETBITULONG64(REGISTER_LOONGARCH64_F28)
                | SETBITULONG64(REGISTER_LOONGARCH64_F29)
                | SETBITULONG64(REGISTER_LOONGARCH64_F30)
                | SETBITULONG64(REGISTER_LOONGARCH64_F31);

    return S_OK;
}

// Reads the value of a single register (identified by a CorDebugRegister index) into *pValue.
// Shared by both GetRegisters overloads to avoid duplicating the per-register mapping.
static HRESULT GetRegisterValue(CordbThread * pThread, const DT_CONTEXT * pContext, int regIndex, CORDB_REGISTER * pValue)
{
    if ((regIndex >= REGISTER_LOONGARCH64_F0) && (regIndex <= REGISTER_LOONGARCH64_F31))
    {
        if (!pThread->m_fFloatStateValid)
        {
            HRESULT     hr = S_OK;
            EX_TRY
            {
                pThread->LoadFloatState();
            }
            EX_CATCH_HRESULT(hr);

            if ( !SUCCEEDED(hr) )
            {
                return hr;
            }
            LOG( ( LF_CORDB, LL_INFO1000, "CRS::GR: Loaded float state\n" ) );
        }

        *pValue = *(CORDB_REGISTER*)&(pThread->m_floatValues[(regIndex - REGISTER_LOONGARCH64_F0)]);
        return S_OK;
    }

    switch (regIndex)
    {
    case REGISTER_LOONGARCH64_A0:
        *pValue = pContext->A0; break;
    case REGISTER_LOONGARCH64_A1:
        *pValue = pContext->A1; break;
    case REGISTER_LOONGARCH64_A2:
        *pValue = pContext->A2; break;
    case REGISTER_LOONGARCH64_A3:
        *pValue = pContext->A3; break;
    case REGISTER_LOONGARCH64_A4:
        *pValue = pContext->A4; break;
    case REGISTER_LOONGARCH64_A5:
        *pValue = pContext->A5; break;
    case REGISTER_LOONGARCH64_A6:
        *pValue = pContext->A6; break;
    case REGISTER_LOONGARCH64_A7:
        *pValue = pContext->A7; break;
    case REGISTER_LOONGARCH64_S0:
        *pValue = pContext->S0; break;
    case REGISTER_LOONGARCH64_S1:
        *pValue = pContext->S1; break;
    case REGISTER_LOONGARCH64_S2:
        *pValue = pContext->S2; break;
    case REGISTER_LOONGARCH64_S3:
        *pValue = pContext->S3; break;
    case REGISTER_LOONGARCH64_S4:
        *pValue = pContext->S4; break;
    case REGISTER_LOONGARCH64_S5:
        *pValue = pContext->S5; break;
    case REGISTER_LOONGARCH64_S6:
        *pValue = pContext->S6; break;
    case REGISTER_LOONGARCH64_S7:
        *pValue = pContext->S7; break;
    case REGISTER_LOONGARCH64_S8:
        *pValue = pContext->S8; break;
    case REGISTER_LOONGARCH64_T0:
        *pValue = pContext->T0; break;
    case REGISTER_LOONGARCH64_T1:
        *pValue = pContext->T1; break;
    case REGISTER_LOONGARCH64_T2:
        *pValue = pContext->T2; break;
    case REGISTER_LOONGARCH64_T3:
        *pValue = pContext->T3; break;
    case REGISTER_LOONGARCH64_T4:
        *pValue = pContext->T4; break;
    case REGISTER_LOONGARCH64_T5:
        *pValue = pContext->T5; break;
    case REGISTER_LOONGARCH64_T6:
        *pValue = pContext->T6; break;
    case REGISTER_LOONGARCH64_T7:
        *pValue = pContext->T7; break;
    case REGISTER_LOONGARCH64_T8:
        *pValue = pContext->T8; break;
    case REGISTER_LOONGARCH64_X0:
        *pValue = pContext->X0; break;
    case REGISTER_LOONGARCH64_PC:
        *pValue = pContext->Pc; break;
    case REGISTER_LOONGARCH64_SP:
        *pValue = pContext->Sp; break;
    case REGISTER_LOONGARCH64_FP:
        *pValue = pContext->Fp; break;
    case REGISTER_LOONGARCH64_RA:
        *pValue = pContext->Ra; break;
    case REGISTER_LOONGARCH64_TP:
        *pValue = pContext->Tp; break;
    default:
        _ASSERTE(false); break;
    }

    return S_OK;
}

HRESULT CordbRegisterSet::GetRegisters(ULONG64 mask, ULONG32 regCount,
                                       CORDB_REGISTER regBuffer[])
{
    PUBLIC_REENTRANT_API_ENTRY(this);
    FAIL_IF_NEUTERED(this);
    ATT_REQUIRE_STOPPED_MAY_FAIL(GetProcess());

    UINT iRegister = 0;

    VALIDATE_POINTER_TO_OBJECT_ARRAY(regBuffer, CORDB_REGISTER, regCount, true, true);

    for (int i = REGISTER_LOONGARCH64_PC;
         i <= REGISTER_LOONGARCH64_F31 && iRegister < regCount;
         i++)
    {
        if (mask &  SETBITULONG64(i))
        {
            _ASSERTE (iRegister < regCount);

            HRESULT hr = GetRegisterValue(m_thread, &m_context, i, &regBuffer[iRegister]);
            if (FAILED(hr))
            {
                return hr;
            }
            iRegister++;
        }
    }

    _ASSERTE (iRegister <= regCount);
    return S_OK;
}


HRESULT CordbRegisterSet::GetRegistersAvailable(ULONG32 regCount,
                                                BYTE    pAvailable[])
{
    FAIL_IF_NEUTERED(this);
    VALIDATE_POINTER_TO_OBJECT_ARRAY(pAvailable, CORDB_REGISTER, regCount, true, true);

    for (int i = 0 ; i < (int)regCount ; ++i)
    {
        if (i * 8 <= REGISTER_LOONGARCH64_F31)
        {
            pAvailable[i] = (i * 8 == REGISTER_LOONGARCH64_F31) ? BYTE(0x1) : BYTE(0xff);
        }
        else
        {
            pAvailable[i] = 0;
        }
    }

    return S_OK;
}


HRESULT CordbRegisterSet::GetRegisters(ULONG32 maskCount, BYTE mask[],
                                       ULONG32 regCount, CORDB_REGISTER regBuffer[])
{
    FAIL_IF_NEUTERED(this);
    VALIDATE_POINTER_TO_OBJECT_ARRAY(regBuffer, CORDB_REGISTER, regCount, true, true);

    UINT iRegister = 0;

    for (int m = 0 ; m < (int)maskCount ; ++m)
    {
        for (int bit = 0 ; bit < 8 ; ++bit)
        {
            if (mask[m] & SETBITULONG64(bit))
            {
                _ASSERTE (iRegister < regCount);

                int i = m * 8 + bit;

                HRESULT hr = GetRegisterValue(m_thread, &m_context, i, &regBuffer[iRegister]);
                if (FAILED(hr))
                {
                    return hr;
                }
                iRegister++;
            }
        }
    }

    return S_OK;
}
