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

            if ((i >= REGISTER_LOONGARCH64_A0) && (i <= REGISTER_LOONGARCH64_X0))
            {
                regBuffer[iRegister++] = *(ULONG64*)((char*)(&m_rd->A0) + ((i - REGISTER_LOONGARCH64_A0) << 3));
                continue;
            }

            if ((i >= REGISTER_LOONGARCH64_S0) && (i <= REGISTER_LOONGARCH64_S8))
            {
                regBuffer[iRegister++] = *(ULONG64*)((char*)(&m_rd->S0) + ((i - REGISTER_LOONGARCH64_S0) << 3));
                continue;
            }

            if ((i >= REGISTER_LOONGARCH64_F0) && (i <= REGISTER_LOONGARCH64_F31))
            {
                if (!m_thread->m_fFloatStateValid)
                {
                    HRESULT     hr = S_OK;
                    EX_TRY
                    {
                        m_thread->LoadFloatState();
                    }
                    EX_CATCH_HRESULT(hr);

                    if ( !SUCCEEDED(hr) )
                    {
                        return hr;
                    }
                    LOG( ( LF_CORDB, LL_INFO1000, "CRS::GR: Loaded float state\n" ) );
                }

                regBuffer[iRegister++] = *(CORDB_REGISTER*)
                                          &(m_thread->m_floatValues[(i - REGISTER_LOONGARCH64_F0)]);
                continue;
            }

            switch (i)
            {
            case REGISTER_LOONGARCH64_PC:
                regBuffer[iRegister++] = m_rd->PC; break;
            case REGISTER_LOONGARCH64_SP:
                regBuffer[iRegister++] = m_rd->SP; break;
            case REGISTER_LOONGARCH64_FP:
                regBuffer[iRegister++] = m_rd->FP; break;
            case REGISTER_LOONGARCH64_RA:
                regBuffer[iRegister++] = m_rd->RA; break;
            case REGISTER_LOONGARCH64_TP:
                regBuffer[iRegister++] = m_rd->TP; break;
            default:
                _ASSERTE(false); break;
            }
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

                if ((i >= REGISTER_LOONGARCH64_A0) && (i <= REGISTER_LOONGARCH64_X0))
                {
                    regBuffer[iRegister++] = *(ULONG64*)((char*)(&m_rd->A0) + ((i - REGISTER_LOONGARCH64_A0) << 3));
                    continue;
                }

                if ((i >= REGISTER_LOONGARCH64_S0) && (i <= REGISTER_LOONGARCH64_S8))
                {
                    regBuffer[iRegister++] = *(ULONG64*)((char*)(&m_rd->S0) + ((i - REGISTER_LOONGARCH64_S0) << 3));
                    continue;
                }


                if ((i >= REGISTER_LOONGARCH64_F0) && (i <= REGISTER_LOONGARCH64_F31))
                {
                    if (!m_thread->m_fFloatStateValid)
                    {
                        HRESULT     hr = S_OK;
                        EX_TRY
                        {
                            m_thread->LoadFloatState();
                        }
                        EX_CATCH_HRESULT(hr);

                        if ( !SUCCEEDED(hr) )
                        {
                            return hr;
                        }
                        LOG( ( LF_CORDB, LL_INFO1000, "CRS::GR: Loaded float state\n" ) );
                    }

                    regBuffer[iRegister++] = *(CORDB_REGISTER*)
                                              &(m_thread->m_floatValues[(i - REGISTER_LOONGARCH64_F0)]);
                    continue;
                }

                switch (i)
                {
                case REGISTER_LOONGARCH64_PC:
                    regBuffer[iRegister++] = m_rd->PC; break;
                case REGISTER_LOONGARCH64_SP:
                    regBuffer[iRegister++] = m_rd->SP; break;
                case REGISTER_LOONGARCH64_FP:
                    regBuffer[iRegister++] = m_rd->FP; break;
                case REGISTER_LOONGARCH64_RA:
                    regBuffer[iRegister++] = m_rd->RA; break;
                case REGISTER_LOONGARCH64_TP:
                    regBuffer[iRegister++] = m_rd->TP; break;
                default:
                    _ASSERTE(false); break;
                }
            }
        }
    }

    return S_OK;
}


// This is just a convenience function to convert a regdisplay into a Context.
// Since a context has more info than a regdisplay, the conversion isn't perfect
// and the context can't be fully accurate.
void CordbRegisterSet::InternalCopyRDToContext(DT_CONTEXT *pInputContext)
{    INTERNAL_SYNC_API_ENTRY(GetProcess());
    _ASSERTE(pInputContext);

    if ((pInputContext->ContextFlags & DT_CONTEXT_INTEGER) == DT_CONTEXT_INTEGER)
    {
        ULONG64* pDest = &pInputContext->A0;
        ULONG64* pSrc  = &m_rd->A0;
        for (int i = REGISTER_LOONGARCH64_A0; i < REGISTER_LOONGARCH64_X0; ++i)
        {
            *pDest++ = *pSrc++;
        }

        pDest = &pInputContext->S0;
        pSrc  = &m_rd->S0;
        for (int i = REGISTER_LOONGARCH64_S0; i <= REGISTER_LOONGARCH64_S8; ++i)
        {
            *pDest++ = *pSrc++;
        }

        pInputContext->Tp = m_rd->TP;
        pInputContext->Ra = m_rd->RA;
    }

    if ((pInputContext->ContextFlags & DT_CONTEXT_CONTROL) == DT_CONTEXT_CONTROL)
    {
        pInputContext->Sp = m_rd->SP;
        pInputContext->Pc = m_rd->PC;
        pInputContext->Fp = m_rd->FP;
    }
}
