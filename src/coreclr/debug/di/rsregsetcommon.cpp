// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//*****************************************************************************
// File: RSRegSetCommon.cpp
//

// Common cross-platform behavior of reg sets. The ICorDebugRegisterSet /
// ICorDebugRegisterSet2 surface is implemented in CordbRegisterSet.cpp using the
// ReadRegistersFromContext / WriteRegistersToContext / GetAvailableRegistersMask
// DDIs; float / SIMD register VALUES come from ReadFloatRegistersFromContext.
//
//*****************************************************************************
#include "stdafx.h"
#include "primitives.h"

/* ------------------------------------------------------------------------- *
 * Common (cross-platform) Register-Set stuff
 * ------------------------------------------------------------------------- */


CordbRegisterSet::CordbRegisterSet(
    const BYTE *         pContextBuffer,
    ULONG32              contextSize,
    CordbThread *        pThread,
    bool fActive,
    bool fQuickUnwind,
    bool fTakeOwnershipOfContext /*= false*/)
  : CordbBase(pThread->GetProcess(), 0, enumCordbRegisterSet)
{
    _ASSERTE( pContextBuffer != NULL );
    _ASSERTE( pThread != NULL );

    m_pContext     = const_cast<BYTE *>(pContextBuffer);
    m_contextSize  = contextSize;
    m_fOwnsContext = fTakeOwnershipOfContext;
    m_thread       = pThread;
    m_active       = fActive;
    m_quickUnwind  = fQuickUnwind;

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

    if (m_fOwnsContext)
    {
        delete[] m_pContext;
    }
    m_pContext = NULL;
    m_contextSize = 0;

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
        if( contextSize < m_contextSize)
        {
            ThrowHR(E_INVALIDARG);
        }

        ValidateOrThrow(context);

        IDacDbiInterface * pDAC = GetProcess()->GetDAC();
        ULONG32 targetContextSize = GetProcess()->GetTargetContextSize();

        BYTE * pLeafContext = NULL;
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
            EX_END_CATCH

            if (pLeafContext != NULL)
            {
                // Raw byte copy of the leaf context, which carries the leaf's ContextFlags into the
                // destination so the flag-sensitive overlay below is gated on those flags.
                memcpy( context, pLeafContext, targetContextSize);
            }
        }

        // Overlay this frame's registers from the cached CONTEXT buffer, honoring the destination's
        // ContextFlags (the leaf's if copied above, otherwise the caller's incoming flags).
        pDAC->CopyContext(context, contextSize, m_pContext, m_contextSize, 0);
    }
    EX_CATCH_HRESULT(hr);
    return hr;
}
