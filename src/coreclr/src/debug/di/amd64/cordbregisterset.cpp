// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
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
                regBuffer[iRegister++] = m_rd->PC; break;
            case REGISTER_STACK_POINTER:
                regBuffer[iRegister++] = m_rd->SP; break;
            case REGISTER_AMD64_RBP:
                regBuffer[iRegister++] = m_rd->Rbp; break;
            case REGISTER_AMD64_RAX:
                regBuffer[iRegister++] = m_rd->Rax; break;
            case REGISTER_AMD64_RBX:
                regBuffer[iRegister++] = m_rd->Rbx; break;
            case REGISTER_AMD64_RCX:
                regBuffer[iRegister++] = m_rd->Rcx; break;
            case REGISTER_AMD64_RDX:
                regBuffer[iRegister++] = m_rd->Rdx; break;
            case REGISTER_AMD64_RSI:
                regBuffer[iRegister++] = m_rd->Rsi; break;
            case REGISTER_AMD64_RDI:
                regBuffer[iRegister++] = m_rd->Rdi; break;
            case REGISTER_AMD64_R8:
                regBuffer[iRegister++] = m_rd->R8;  break;
            case REGISTER_AMD64_R9:
                regBuffer[iRegister++] = m_rd->R9;  break;
            case REGISTER_AMD64_R10:
                regBuffer[iRegister++] = m_rd->R10; break;
            case REGISTER_AMD64_R11:
                regBuffer[iRegister++] = m_rd->R11; break;
            case REGISTER_AMD64_R12:
                regBuffer[iRegister++] = m_rd->R12; break;
            case REGISTER_AMD64_R13:
                regBuffer[iRegister++] = m_rd->R13; break;
            case REGISTER_AMD64_R14:
                regBuffer[iRegister++] = m_rd->R14; break;
            case REGISTER_AMD64_R15:
                regBuffer[iRegister++] = m_rd->R15; break;

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


// This is just a convenience function to convert a regdisplay into a Context.
// Since a context has more info than a regdisplay, the conversion isn't perfect
// and the context can't be fully accurate.
void CordbRegisterSet::InternalCopyRDToContext(DT_CONTEXT *pInputContext)
{
    INTERNAL_SYNC_API_ENTRY(GetProcess());
    _ASSERTE(pInputContext);

    if((pInputContext->ContextFlags & DT_CONTEXT_INTEGER)==DT_CONTEXT_INTEGER)
    {
        pInputContext->Rax = m_rd->Rax;
        pInputContext->Rbx = m_rd->Rbx;
        pInputContext->Rcx = m_rd->Rcx;
        pInputContext->Rdx = m_rd->Rdx;
        pInputContext->Rbp = m_rd->Rbp;
        pInputContext->Rsi = m_rd->Rsi;
        pInputContext->Rdi = m_rd->Rdi;
        pInputContext->R8  = m_rd->R8;
        pInputContext->R9  = m_rd->R9;
        pInputContext->R10 = m_rd->R10;
        pInputContext->R11 = m_rd->R11;
        pInputContext->R12 = m_rd->R12;
        pInputContext->R13 = m_rd->R13;
        pInputContext->R14 = m_rd->R14;
        pInputContext->R15 = m_rd->R15;
    }


    if((pInputContext->ContextFlags & DT_CONTEXT_CONTROL)==DT_CONTEXT_CONTROL)
    {
        pInputContext->Rip = m_rd->PC;
        pInputContext->Rsp = m_rd->SP;
    }
}
