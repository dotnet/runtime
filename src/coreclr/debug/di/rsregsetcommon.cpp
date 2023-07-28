// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//*****************************************************************************
// File: RSRegSetCommon.cpp
//

// Common cross-platform behavior of reg sets.
// Platform specific stuff is in CordbRegisterSet.cpp located in
// the platform sub-dir.
//
//*****************************************************************************
#include "stdafx.h"
#include "primitives.h"

/* ------------------------------------------------------------------------- *
 * Common (cross-platform) Register-Set stuff
 * ------------------------------------------------------------------------- */


CordbRegisterSet::CordbRegisterSet(
    DebuggerREGDISPLAY * pRegDisplay,
    CordbThread *        pThread,
    bool fActive,
    bool fQuickUnwind,
    bool fTakeOwnershipOfDRD /*= false*/)
  : CordbBase(pThread->GetProcess(), 0, enumCordbRegisterSet)
{
    _ASSERTE( pRegDisplay != NULL );
    _ASSERTE( pThread != NULL );
    m_rd          = pRegDisplay;
    m_thread      = pThread;
    m_active      = fActive;
    m_quickUnwind = fQuickUnwind;

    m_fTakeOwnershipOfDRD = fTakeOwnershipOfDRD;

    // Add to our parent thread's neuter list.

    HRESULT hr = S_OK;
    EX_TRY
    {
        pThread->GetRefreshStackNeuterList()->Add(GetProcess(), this);
    }
    EX_CATCH_HRESULT(hr);
    SetUnrecoverableIfFailed(GetProcess(), hr);
}

void CordbRegisterSet::Neuter()
{
    m_thread = NULL;
    if (m_fTakeOwnershipOfDRD)
    {
        delete m_rd;
    }
    m_rd = NULL;

    CordbBase::Neuter();
}

CordbRegisterSet::~CordbRegisterSet()
{
    _ASSERTE(this->IsNeutered());
}


HRESULT CordbRegisterSet::QueryInterface(REFIID riid, void **ppInterface)
{
    // <NOTE>
    // This is an exception to the rule that a QI for a higher version API should fail if
    // the debugger does not support that version of the API.  The reasoning is that
    // while higher versions of other APIs support enhanced functionality and are not
    // required, this particular API is required on IA64.  An example scenario is when an
    // Everett debuggger is ported to Whidbey and the user wants to use the debugger on IA64.
    // The user should not be required to implement the ICorDebugManagedCallback2 API, as would
    // be the case if we make the versioning check like other higher version APIs.
    // </NOTE>
    if (riid == IID_ICorDebugRegisterSet)
    {
        *ppInterface = static_cast<ICorDebugRegisterSet*>(this);
    }
    else if (riid == IID_ICorDebugRegisterSet2)
    {
        *ppInterface = static_cast<ICorDebugRegisterSet2*>(this);
    }
    else if (riid == IID_IUnknown)
    {
        *ppInterface = static_cast<IUnknown*>(static_cast<ICorDebugRegisterSet*>(this));
    }
    else
    {
        *ppInterface = NULL;
        return E_NOINTERFACE;
    }

    ExternalAddRef();
    return S_OK;
}

//-----------------------------------------------------------------------------
// This is just a convenience function to convert a regdisplay into a Context.
// Since a context has more info than a regdisplay, the conversion isn't perfect
// and the context can't be fully accurate.
//
// Inputs:
//    contextSize - sizeof incoming context buffer  in bytes
//    context - buffer to copy this regdisplay's OS CONTEXT structure into.
//
// Returns S_OK on success.
//-----------------------------------------------------------------------------
HRESULT CordbRegisterSet::GetThreadContext(ULONG32 contextSize, BYTE context[])
{
    PUBLIC_REENTRANT_API_ENTRY(this);
    FAIL_IF_NEUTERED(this);
    ATT_REQUIRE_STOPPED_MAY_FAIL(GetProcess());

    HRESULT hr = S_OK;
    EX_TRY
    {
        _ASSERTE( m_thread != NULL );
        if( contextSize < sizeof( DT_CONTEXT ))
        {
            ThrowHR(E_INVALIDARG);
        }

        ValidateOrThrow(context);

        DT_CONTEXT *pInputContext = reinterpret_cast<DT_CONTEXT *> (context);

        // Just to be safe, zero out the buffer we got in while preserving the ContextFlags.
        // On X64 the ContextFlags field is not the first 4 bytes of the DT_CONTEXT.
        DWORD dwContextFlags = pInputContext->ContextFlags;
        ZeroMemory(context, contextSize);
        pInputContext->ContextFlags = dwContextFlags;

        // Augment the leafmost (active) register w/ information from the current context.
        DT_CONTEXT * pLeafContext = NULL;
        if (m_active)
        {
            EX_TRY
            {
                // This may fail, but it is not a disastrous failure in this case.  All we care is whether
                // pLeafContext is updated to a non-NULL value.
                m_thread->GetManagedContext( &pLeafContext);
            }
            EX_CATCH
            {
            }
            EX_END_CATCH(SwallowAllExceptions)

            if (pLeafContext != NULL)
            {
                // @todo - shouldn't this be a context-flags sensitive copy?
                memmove( pInputContext, pLeafContext, sizeof( DT_CONTEXT) );
            }
        }


        // Now update the registers based on the current frame.
        // This is a very platform specific action.
        InternalCopyRDToContext(pInputContext);
    }
    EX_CATCH_HRESULT(hr);
    return hr;
}

//-----------------------------------------------------------------------------
// Helpers to impl IRegSet2 on top of original IRegSet.
// These are useful on platforms that don't need IRegSet2 (like x86 + amd64).
// See CorDebug.idl for details.
//
// Inputs:
//   regCount - size of pAvailable buffer in bytes
//   pAvailable - buffer to hold bitvector of available registers.
//                On success, bit at position CorDebugRegister is 1 iff that
//                register is available.
// Returns S_OK on success.
//-----------------------------------------------------------------------------
HRESULT CordbRegisterSet::GetRegistersAvailableAdapter(
    ULONG32 regCount,
    BYTE    pAvailable[])
{
    // Defer to call on v1.0 interface
    HRESULT hr = S_OK;

    if (regCount < sizeof(ULONG64))
    {
        return E_INVALIDARG;
    }

    _ASSERTE(pAvailable != NULL);

    ULONG64 availRegs;
    hr = this->GetRegistersAvailable(&availRegs);
    if (FAILED(hr))
    {
        return hr;
    }

    // Nor marshal our 64-bit value into the outgoing byte array.
    for(int iBit = 0; iBit < (int) sizeof(availRegs) * 8; iBit++)
    {
        ULONG64 test = SETBITULONG64(iBit);
        if (availRegs & test)
        {
            SET_BIT_MASK(pAvailable, iBit);
        }
        else
        {
            RESET_BIT_MASK(pAvailable, iBit);
        }
    }
    return S_OK;
}

//-----------------------------------------------------------------------------
// Helpers to impl IRegSet2 on top of original IRegSet.
// These are useful on platforms that don't need IRegSet2 (like x86 + amd64).
// See CorDebug.idl for details.
//
// Inputs:
//  maskCount - size of mask buffer in bytes.
//  mask - input buffer specifying registers to request
//  regCount - size of regBuffer in bytes
//  regBuffer - output buffer, regBuffer[n] = value of register at n-th active
//              bit in mask.
// Returns S_OK on success.
//-----------------------------------------------------------------------------

// mask input request registers, which get written to regCount buffer.
HRESULT CordbRegisterSet::GetRegistersAdapter(
    ULONG32 maskCount, BYTE mask[],
    ULONG32 regCount, CORDB_REGISTER regBuffer[])
{
    // Convert input mask to orig mask.
    ULONG64 maskOrig = 0;

    for(UINT iBit = 0; iBit < maskCount * 8; iBit++)
    {
        if (IS_SET_BIT_MASK(mask, iBit))
        {
            maskOrig |= SETBITULONG64(iBit);
        }
    }

    return this->GetRegisters(maskOrig,
        regCount, regBuffer);
}
