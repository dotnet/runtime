// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// ============================================================
//
// BindResult.inl
//


//
// Implements the inline methods of BindResult
//
// ============================================================

#ifndef __BINDER__BIND_RESULT_INL__
#define __BINDER__BIND_RESULT_INL__

#include "contextentry.hpp"
#include "assembly.hpp"

namespace BINDER_SPACE
{
BindResult::BindResult()
{
    m_dwResultFlags = ContextEntry::RESULT_FLAG_NONE;
    m_pAssemblyName = NULL;
    m_pAssembly = NULL;
}

BindResult::~BindResult()
{
    SAFE_RELEASE(m_pAssemblyName);
}

AssemblyName *BindResult::GetAssemblyName(BOOL fAddRef /* = FALSE */)
{
    AssemblyName *pAssemblyName = m_pAssemblyName;

    if (fAddRef && (pAssemblyName != NULL))
    {
        pAssemblyName->AddRef();
    }

    return pAssemblyName;
}

Assembly *BindResult::GetAssembly(BOOL fAddRef /* = FALSE */)
{
    Assembly* pAssembly = m_pAssembly;

    if (fAddRef && (pAssembly != NULL))
    {
        pAssembly->AddRef();
    }

    return pAssembly;
}

BOOL BindResult::GetIsInTPA()
{
    return ((m_dwResultFlags & ContextEntry::RESULT_FLAG_IS_IN_TPA) != 0);
}

void BindResult::SetIsInTPA(BOOL fIsInTPA)
{
    if (fIsInTPA)
    {
        m_dwResultFlags |= ContextEntry::RESULT_FLAG_IS_IN_TPA;
    }
    else
    {
        m_dwResultFlags &= ~ContextEntry::RESULT_FLAG_IS_IN_TPA;
    }
}

BOOL BindResult::GetIsContextBound()
{
    return ((m_dwResultFlags & ContextEntry::RESULT_FLAG_CONTEXT_BOUND) != 0);
}

void BindResult::SetIsContextBound(BOOL fIsContextBound)
{
    if (fIsContextBound)
    {
        m_dwResultFlags |= ContextEntry::RESULT_FLAG_CONTEXT_BOUND;
    }
    else
    {
        m_dwResultFlags &= ~ContextEntry::RESULT_FLAG_CONTEXT_BOUND;
    }
}

BOOL BindResult::GetIsFirstRequest()
{
    return ((m_dwResultFlags & ContextEntry::RESULT_FLAG_FIRST_REQUEST) != 0);
}

void BindResult::SetIsFirstRequest(BOOL fIsFirstRequest)
{
    if (fIsFirstRequest)
    {
        m_dwResultFlags |= ContextEntry::RESULT_FLAG_FIRST_REQUEST;
    }
    else
    {
        m_dwResultFlags &= ~ContextEntry::RESULT_FLAG_FIRST_REQUEST;
    }
}

void BindResult::SetResult(ContextEntry *pContextEntry, BOOL fIsContextBound /* = TRUE */)
{
    _ASSERTE(pContextEntry != NULL);

    SetIsInTPA(pContextEntry->GetIsInTPA());
    SetIsContextBound(fIsContextBound);
    SAFE_RELEASE(m_pAssemblyName);
    m_pAssemblyName = pContextEntry->GetAssemblyName(TRUE /* fAddRef */);
    m_pAssembly = pContextEntry->GetAssembly(TRUE /* fAddRef */);
}

void BindResult::SetResult(Assembly *pAssembly)
{
    _ASSERTE(pAssembly != NULL);

    SetIsInTPA(pAssembly->GetIsInTPA());
    SAFE_RELEASE(m_pAssemblyName);
    m_pAssemblyName = pAssembly->GetAssemblyName(TRUE /* fAddRef */);
    pAssembly->AddRef();
    m_pAssembly = pAssembly;
}

void BindResult::SetResult(BindResult *pBindResult)
{
    _ASSERTE(pBindResult != NULL);

    m_dwResultFlags = pBindResult->m_dwResultFlags;
    SAFE_RELEASE(m_pAssemblyName);
    m_pAssemblyName = pBindResult->GetAssemblyName(TRUE /* fAddRef */);
    m_pAssembly = pBindResult->GetAssembly(TRUE /* fAddRef */);

    const AttemptResult *attempt = pBindResult->GetAttempt(true /*foundInContext*/);
    if (attempt != nullptr)
        m_inContextAttempt.Set(attempt);

    attempt = pBindResult->GetAttempt(false /*foundInContext*/);
    if (attempt != nullptr)
        m_applicationAssembliesAttempt.Set(attempt);
}

void BindResult::SetNoResult()
{
    m_pAssemblyName = NULL;
}

BOOL BindResult::HaveResult()
{
    return (GetAssemblyName() != NULL);
}

void BindResult::Reset()
{
    SAFE_RELEASE(m_pAssemblyName);
    m_pAssembly = NULL;
    m_dwResultFlags = ContextEntry::RESULT_FLAG_NONE;
    m_inContextAttempt.Reset();
    m_applicationAssembliesAttempt.Reset();
}

void BindResult::SetAttemptResult(HRESULT hr, ContextEntry *pContextEntry)
{
    Assembly *assembly = nullptr;
    if (pContextEntry != nullptr)
        assembly = pContextEntry->GetAssembly(TRUE /* fAddRef */);

    m_inContextAttempt.Assembly = assembly;
    m_inContextAttempt.HResult = hr;
    m_inContextAttempt.Attempted = true;
}

void BindResult::SetAttemptResult(HRESULT hr, Assembly *pAssembly)
{
    if (pAssembly != nullptr)
        pAssembly->AddRef();

    m_applicationAssembliesAttempt.Assembly = pAssembly;
    m_applicationAssembliesAttempt.HResult = hr;
    m_applicationAssembliesAttempt.Attempted = true;
}

const BindResult::AttemptResult* BindResult::GetAttempt(bool foundInContext) const
{
    const BindResult::AttemptResult &result = foundInContext ? m_inContextAttempt : m_applicationAssembliesAttempt;
    return result.Attempted ? &result : nullptr;
}

void BindResult::AttemptResult::Set(const BindResult::AttemptResult *result)
{
    BINDER_SPACE::Assembly *assembly = result->Assembly;
    if (assembly != nullptr)
        assembly->AddRef();

    Assembly = assembly;
    HResult = result->HResult;
    Attempted = result->Attempted;
}

}
#endif
