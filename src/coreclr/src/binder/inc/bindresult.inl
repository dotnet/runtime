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
    m_pIUnknownAssembly = NULL;
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

IUnknown *BindResult::GetAssembly(BOOL fAddRef /* = FALSE */)
{
    IUnknown *pIUnknownAssembly = m_pIUnknownAssembly;

    if (fAddRef && (pIUnknownAssembly != NULL))
    {
        pIUnknownAssembly->AddRef();
    }

    return pIUnknownAssembly;
}

Assembly *BindResult::GetAsAssembly(BOOL fAddRef /* = FALSE */)
{
    return static_cast<Assembly *>(GetAssembly(fAddRef));
}

BOOL BindResult::GetIsInGAC()
{
    return ((m_dwResultFlags & ContextEntry::RESULT_FLAG_IS_IN_GAC) != 0);
}

void BindResult::SetIsInGAC(BOOL fIsInGAC)
{
    if (fIsInGAC)
    {
        m_dwResultFlags |= ContextEntry::RESULT_FLAG_IS_IN_GAC;
    }
    else
    {
        m_dwResultFlags &= ~ContextEntry::RESULT_FLAG_IS_IN_GAC;
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

    SetIsInGAC(pContextEntry->GetIsInGAC());
    SetIsContextBound(fIsContextBound);
    SAFE_RELEASE(m_pAssemblyName);
    m_pAssemblyName = pContextEntry->GetAssemblyName(TRUE /* fAddRef */);
    m_pIUnknownAssembly = pContextEntry->GetAssembly(TRUE /* fAddRef */);
}

void BindResult::SetResult(Assembly *pAssembly)
{
    _ASSERTE(pAssembly != NULL);

    SetIsInGAC(pAssembly->GetIsInGAC());
    SAFE_RELEASE(m_pAssemblyName);
    m_pAssemblyName = pAssembly->GetAssemblyName(TRUE /* fAddRef */);
    pAssembly->AddRef();
    m_pIUnknownAssembly = static_cast<IUnknown *>(pAssembly);
}

void BindResult::SetResult(BindResult *pBindResult)
{
    _ASSERTE(pBindResult != NULL);

    m_dwResultFlags = pBindResult->m_dwResultFlags;
    SAFE_RELEASE(m_pAssemblyName);
    m_pAssemblyName = pBindResult->GetAssemblyName(TRUE /* fAddRef */);
    m_pIUnknownAssembly = pBindResult->GetAssembly(TRUE /* fAddRef */);

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
    m_pIUnknownAssembly = NULL;
    m_dwResultFlags = ContextEntry::RESULT_FLAG_NONE;
    m_inContextAttempt.Reset();
    m_applicationAssembliesAttempt.Reset();
}

void BindResult::SetAttemptResult(HRESULT hr, ContextEntry *pContextEntry)
{
    Assembly *assembly = nullptr;
    if (pContextEntry != nullptr)
        assembly = static_cast<Assembly *>(pContextEntry->GetAssembly(TRUE /* fAddRef */));

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
