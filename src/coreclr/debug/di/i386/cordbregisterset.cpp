// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
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

    (*pAvailable) = SETBITULONG64( REGISTER_INSTRUCTION_POINTER )
            |   SETBITULONG64( REGISTER_STACK_POINTER )
            |   SETBITULONG64( REGISTER_FRAME_POINTER );

    if (!m_quickUnwind || m_active)
        (*pAvailable) |= SETBITULONG64( REGISTER_X86_EAX )
            |   SETBITULONG64( REGISTER_X86_ECX )
            |   SETBITULONG64( REGISTER_X86_EDX )
            |   SETBITULONG64( REGISTER_X86_EBX )
            |   SETBITULONG64( REGISTER_X86_ESI )
            |   SETBITULONG64( REGISTER_X86_EDI );

    if (m_active)
        (*pAvailable) |= SETBITULONG64( REGISTER_X86_FPSTACK_0 )
            |   SETBITULONG64( REGISTER_X86_FPSTACK_1 )
            |   SETBITULONG64( REGISTER_X86_FPSTACK_2 )
            |   SETBITULONG64( REGISTER_X86_FPSTACK_3 )
            |   SETBITULONG64( REGISTER_X86_FPSTACK_4 )
            |   SETBITULONG64( REGISTER_X86_FPSTACK_5 )
            |   SETBITULONG64( REGISTER_X86_FPSTACK_6 )
            |   SETBITULONG64( REGISTER_X86_FPSTACK_7 );

    return S_OK;
}


#define FPSTACK_FROM_INDEX( _index )  (m_thread->m_floatValues[m_thread->m_floatStackTop -( (REGISTER_X86_FPSTACK_##_index)-REGISTER_X86_FPSTACK_0 ) ] )

HRESULT CordbRegisterSet::GetRegisters(ULONG64 mask, ULONG32 regCount,
                                       CORDB_REGISTER regBuffer[])
{
    PUBLIC_REENTRANT_API_ENTRY(this);
    FAIL_IF_NEUTERED(this);
    ATT_REQUIRE_STOPPED_MAY_FAIL(GetProcess());

    UINT iRegister = 0;

    VALIDATE_POINTER_TO_OBJECT_ARRAY(regBuffer, CORDB_REGISTER, regCount, true, true);

    //If we need some floating point value, tell the thread to get it
    if ( mask & (       SETBITULONG64(REGISTER_X86_FPSTACK_0)
                    |   SETBITULONG64(REGISTER_X86_FPSTACK_1)
                    |   SETBITULONG64(REGISTER_X86_FPSTACK_2)
                    |   SETBITULONG64(REGISTER_X86_FPSTACK_3)
                    |   SETBITULONG64(REGISTER_X86_FPSTACK_4)
                    |   SETBITULONG64(REGISTER_X86_FPSTACK_5)
                    |   SETBITULONG64(REGISTER_X86_FPSTACK_6)
                    |   SETBITULONG64(REGISTER_X86_FPSTACK_7 ) ) )
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
    if ( mask & (       SETBITULONG64( REGISTER_X86_EAX )
                    |   SETBITULONG64( REGISTER_X86_ECX )
                    |   SETBITULONG64( REGISTER_X86_EDX )
                    |   SETBITULONG64( REGISTER_X86_EBX )
                    |   SETBITULONG64( REGISTER_X86_ESI )
                    |   SETBITULONG64( REGISTER_X86_EDI ) ) )
    {
        if (!m_active && m_quickUnwind)
            return E_INVALIDARG;
    }

    for ( int i = REGISTER_INSTRUCTION_POINTER
        ; i<=REGISTER_X86_FPSTACK_7 && iRegister < regCount
        ; i++)
    {
        if( mask &  SETBITULONG64(i) )
        {
            switch( i )
            {
            case REGISTER_INSTRUCTION_POINTER:
                regBuffer[iRegister++] = m_context.Eip; break;
            case REGISTER_STACK_POINTER:
                regBuffer[iRegister++] = m_context.Esp; break;
            case REGISTER_FRAME_POINTER:
                regBuffer[iRegister++] = m_context.Ebp; break;
            case REGISTER_X86_EAX:
                regBuffer[iRegister++] = m_context.Eax; break;
            case REGISTER_X86_EBX:
                regBuffer[iRegister++] = m_context.Ebx; break;
            case REGISTER_X86_ECX:
                regBuffer[iRegister++] = m_context.Ecx; break;
            case REGISTER_X86_EDX:
                regBuffer[iRegister++] = m_context.Edx; break;
            case REGISTER_X86_ESI:
                regBuffer[iRegister++] = m_context.Esi; break;
            case REGISTER_X86_EDI:
                regBuffer[iRegister++] = m_context.Edi; break;

            //for floats, copy the bits, not the integer part of
            //the value, into the register
            case REGISTER_X86_FPSTACK_0:
                memcpy(&regBuffer[iRegister++],
                       &(FPSTACK_FROM_INDEX(0)),
                       sizeof(CORDB_REGISTER));
                break;
            case REGISTER_X86_FPSTACK_1:
                memcpy( &regBuffer[iRegister++],
                    & (FPSTACK_FROM_INDEX( 1 ) ),
                    sizeof(CORDB_REGISTER) );
                break;
            case    REGISTER_X86_FPSTACK_2:
                memcpy( &regBuffer[iRegister++],
                    & (FPSTACK_FROM_INDEX( 2 ) ),
                    sizeof(CORDB_REGISTER) ); break;
            case    REGISTER_X86_FPSTACK_3:
                memcpy( &regBuffer[iRegister++],
                    & (FPSTACK_FROM_INDEX( 3 ) ),
                    sizeof(CORDB_REGISTER) ); break;
            case    REGISTER_X86_FPSTACK_4:
                memcpy( &regBuffer[iRegister++],
                    & (FPSTACK_FROM_INDEX( 4 ) ),
                    sizeof(CORDB_REGISTER) ); break;
            case    REGISTER_X86_FPSTACK_5:
                memcpy( &regBuffer[iRegister++],
                    & (FPSTACK_FROM_INDEX( 5 ) ),
                    sizeof(CORDB_REGISTER) ); break;
            case    REGISTER_X86_FPSTACK_6:
                memcpy( &regBuffer[iRegister++],
                    & (FPSTACK_FROM_INDEX( 6 ) ),
                    sizeof(CORDB_REGISTER) ); break;
            case    REGISTER_X86_FPSTACK_7:
                memcpy( &regBuffer[iRegister++],
                    & (FPSTACK_FROM_INDEX( 7 ) ),
                    sizeof(CORDB_REGISTER) ); break;
            }
        }
    }

    _ASSERTE( iRegister <= regCount );
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
