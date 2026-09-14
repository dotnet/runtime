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
    VALIDATE_POINTER_TO_OBJECT(pAvailable, ULONG64*);

    (*pAvailable) = SETBITULONG64( REGISTER_INSTRUCTION_POINTER )
            |   SETBITULONG64( REGISTER_STACK_POINTER );

    if (!m_quickUnwind || m_active)
        (*pAvailable) |= SETBITULONG64( REGISTER_AMD64_RBP )
            |   SETBITULONG64( REGISTER_AMD64_RAX )
            |   SETBITULONG64( REGISTER_AMD64_RCX )
            |   SETBITULONG64( REGISTER_AMD64_RDX )
            |   SETBITULONG64( REGISTER_AMD64_RBX )
            |   SETBITULONG64( REGISTER_AMD64_RSI )
            |   SETBITULONG64( REGISTER_AMD64_RDI )
            |   SETBITULONG64( REGISTER_AMD64_R8  )
            |   SETBITULONG64( REGISTER_AMD64_R9  )
            |   SETBITULONG64( REGISTER_AMD64_R10 )
            |   SETBITULONG64( REGISTER_AMD64_R11 )
            |   SETBITULONG64( REGISTER_AMD64_R12 )
            |   SETBITULONG64( REGISTER_AMD64_R13 )
            |   SETBITULONG64( REGISTER_AMD64_R14 )
            |   SETBITULONG64( REGISTER_AMD64_R15 );

    if (m_active)
        (*pAvailable) |= SETBITULONG64( REGISTER_AMD64_XMM0  )
            |   SETBITULONG64( REGISTER_AMD64_XMM1  )
            |   SETBITULONG64( REGISTER_AMD64_XMM2  )
            |   SETBITULONG64( REGISTER_AMD64_XMM3  )
            |   SETBITULONG64( REGISTER_AMD64_XMM4  )
            |   SETBITULONG64( REGISTER_AMD64_XMM5  )
            |   SETBITULONG64( REGISTER_AMD64_XMM6  )
            |   SETBITULONG64( REGISTER_AMD64_XMM7  )
            |   SETBITULONG64( REGISTER_AMD64_XMM8  )
            |   SETBITULONG64( REGISTER_AMD64_XMM9  )
            |   SETBITULONG64( REGISTER_AMD64_XMM10 )
            |   SETBITULONG64( REGISTER_AMD64_XMM11 )
            |   SETBITULONG64( REGISTER_AMD64_XMM12 )
            |   SETBITULONG64( REGISTER_AMD64_XMM13 )
            |   SETBITULONG64( REGISTER_AMD64_XMM14 )
            |   SETBITULONG64( REGISTER_AMD64_XMM15 );

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

    if ( mask & (       SETBITULONG64( REGISTER_AMD64_XMM0  )
                    |   SETBITULONG64( REGISTER_AMD64_XMM1  )
                    |   SETBITULONG64( REGISTER_AMD64_XMM2  )
                    |   SETBITULONG64( REGISTER_AMD64_XMM3  )
                    |   SETBITULONG64( REGISTER_AMD64_XMM4  )
                    |   SETBITULONG64( REGISTER_AMD64_XMM5  )
                    |   SETBITULONG64( REGISTER_AMD64_XMM6  )
                    |   SETBITULONG64( REGISTER_AMD64_XMM7  )
                    |   SETBITULONG64( REGISTER_AMD64_XMM8  )
                    |   SETBITULONG64( REGISTER_AMD64_XMM9  )
                    |   SETBITULONG64( REGISTER_AMD64_XMM10 )
                    |   SETBITULONG64( REGISTER_AMD64_XMM11 )
                    |   SETBITULONG64( REGISTER_AMD64_XMM12 )
                    |   SETBITULONG64( REGISTER_AMD64_XMM13 )
                    |   SETBITULONG64( REGISTER_AMD64_XMM14 )
                    |   SETBITULONG64( REGISTER_AMD64_XMM15 ) ) )
    {
        HRESULT     hr = S_OK;

        if (!m_active)
            return E_INVALIDARG;

        if (!m_thread->m_fFloatStateValid)
        {
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
    }

    // Make sure that the registers are really available
    if ( mask & (       SETBITULONG64( REGISTER_AMD64_RBP )
                    |   SETBITULONG64( REGISTER_AMD64_RAX )
                    |   SETBITULONG64( REGISTER_AMD64_RCX )
                    |   SETBITULONG64( REGISTER_AMD64_RDX )
                    |   SETBITULONG64( REGISTER_AMD64_RBX )
                    |   SETBITULONG64( REGISTER_AMD64_RSI )
                    |   SETBITULONG64( REGISTER_AMD64_RDI )
                    |   SETBITULONG64( REGISTER_AMD64_R8  )
                    |   SETBITULONG64( REGISTER_AMD64_R9  )
                    |   SETBITULONG64( REGISTER_AMD64_R10 )
                    |   SETBITULONG64( REGISTER_AMD64_R11 )
                    |   SETBITULONG64( REGISTER_AMD64_R12 )
                    |   SETBITULONG64( REGISTER_AMD64_R13 )
                    |   SETBITULONG64( REGISTER_AMD64_R14 )
                    |   SETBITULONG64( REGISTER_AMD64_R15 ) ) )
    {
        if (!m_active && m_quickUnwind)
            return E_INVALIDARG;
    }

    for ( int i = REGISTER_INSTRUCTION_POINTER
        ; i<=REGISTER_AMD64_XMM15 && iRegister < regCount
        ; i++)
    {
        if( mask &  SETBITULONG64(i) )
        {
            switch( i )
            {
            case REGISTER_INSTRUCTION_POINTER:
                regBuffer[iRegister++] = m_context.Rip; break;
            case REGISTER_STACK_POINTER:
                regBuffer[iRegister++] = m_context.Rsp; break;
            case REGISTER_AMD64_RBP:
                regBuffer[iRegister++] = m_context.Rbp; break;
            case REGISTER_AMD64_RAX:
                regBuffer[iRegister++] = m_context.Rax; break;
            case REGISTER_AMD64_RBX:
                regBuffer[iRegister++] = m_context.Rbx; break;
            case REGISTER_AMD64_RCX:
                regBuffer[iRegister++] = m_context.Rcx; break;
            case REGISTER_AMD64_RDX:
                regBuffer[iRegister++] = m_context.Rdx; break;
            case REGISTER_AMD64_RSI:
                regBuffer[iRegister++] = m_context.Rsi; break;
            case REGISTER_AMD64_RDI:
                regBuffer[iRegister++] = m_context.Rdi; break;
            case REGISTER_AMD64_R8:
                regBuffer[iRegister++] = m_context.R8;  break;
            case REGISTER_AMD64_R9:
                regBuffer[iRegister++] = m_context.R9;  break;
            case REGISTER_AMD64_R10:
                regBuffer[iRegister++] = m_context.R10; break;
            case REGISTER_AMD64_R11:
                regBuffer[iRegister++] = m_context.R11; break;
            case REGISTER_AMD64_R12:
                regBuffer[iRegister++] = m_context.R12; break;
            case REGISTER_AMD64_R13:
                regBuffer[iRegister++] = m_context.R13; break;
            case REGISTER_AMD64_R14:
                regBuffer[iRegister++] = m_context.R14; break;
            case REGISTER_AMD64_R15:
                regBuffer[iRegister++] = m_context.R15; break;

            case    REGISTER_AMD64_XMM0:
            case    REGISTER_AMD64_XMM1:
            case    REGISTER_AMD64_XMM2:
            case    REGISTER_AMD64_XMM3:
            case    REGISTER_AMD64_XMM4:
            case    REGISTER_AMD64_XMM5:
            case    REGISTER_AMD64_XMM6:
            case    REGISTER_AMD64_XMM7:
            case    REGISTER_AMD64_XMM8:
            case    REGISTER_AMD64_XMM9:
            case    REGISTER_AMD64_XMM10:
            case    REGISTER_AMD64_XMM11:
            case    REGISTER_AMD64_XMM12:
            case    REGISTER_AMD64_XMM13:
            case    REGISTER_AMD64_XMM14:
            case    REGISTER_AMD64_XMM15:
                regBuffer[iRegister++] = *(CORDB_REGISTER*)
                                          &(m_thread->m_floatValues[(i - REGISTER_AMD64_XMM0)]);
                break;
            }
        }
    }

    _ASSERTE( iRegister <= regCount );
    return S_OK;
}


HRESULT CordbRegisterSet::GetRegistersAvailable(ULONG32 regCount,
                                                BYTE    pAvailable[])
{
    FAIL_IF_NEUTERED(this);
    VALIDATE_POINTER_TO_OBJECT_ARRAY(pAvailable, CORDB_REGISTER, regCount, true, true);

    // Defer to adapter for v1.0 interface
    return GetRegistersAvailableAdapter(regCount, pAvailable);
}


HRESULT CordbRegisterSet::GetRegisters(ULONG32 maskCount, BYTE mask[],
                                       ULONG32 regCount, CORDB_REGISTER regBuffer[])
{
    FAIL_IF_NEUTERED(this);
    VALIDATE_POINTER_TO_OBJECT_ARRAY(regBuffer, CORDB_REGISTER, regCount, true, true);

    // Defer to adapter for v1.0 interface
    return GetRegistersAdapter(maskCount, mask, regCount, regBuffer);
}
