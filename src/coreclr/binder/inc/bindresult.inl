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

#include "assembly.hpp"

namespace BINDER_SPACE
{
BindResult::BindResult()
{
    m_isContextBound = false;
    m_pAssembly = NULL;
}

AssemblyName *BindResult::GetAssemblyName(BOOL fAddRef /* = FALSE */)
{
    Assembly *pAssembly = m_pAssembly;
    if (pAssembly == NULL)
        return NULL;

    return pAssembly->GetAssemblyName(fAddRef);
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

BOOL BindResult::GetIsContextBound()
{
    return m_isContextBound;
}

void BindResult::SetResult(Assembly *pAssembly, bool isInContext)
{
    _ASSERTE(pAssembly != NULL);

    pAssembly->AddRef();
    m_pAssembly = pAssembly;
    m_isContextBound = isInContext;
}

void BindResult::SetResult(BindResult *pBindResult)
{
    _ASSERTE(pBindResult != NULL);

    m_isContextBound = pBindResult->m_isContextBound;
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
    m_pAssembly = NULL;
}

BOOL BindResult::HaveResult()
{
    return m_pAssembly != NULL;
}

void BindResult::Reset()
{
    m_pAssembly = NULL;
    m_isContextBound = false;
    m_inContextAttempt.Reset();
    m_applicationAssembliesAttempt.Reset();
}

void BindResult::SetAttemptResult(HRESULT hr, Assembly *pAssembly, bool isInContext)
{
    if (pAssembly != nullptr)
        pAssembly->AddRef();

    BindResult::AttemptResult &result = isInContext ? m_inContextAttempt : m_applicationAssembliesAttempt;
    result.AssemblyHolder = pAssembly;
    result.HResult = hr;
    result.Attempted = true;
}

const BindResult::AttemptResult* BindResult::GetAttempt(bool foundInContext) const
{
    const BindResult::AttemptResult &result = foundInContext ? m_inContextAttempt : m_applicationAssembliesAttempt;
    return result.Attempted ? &result : nullptr;
}

void BindResult::AttemptResult::Set(const BindResult::AttemptResult *result)
{
    BINDER_SPACE::Assembly *assembly = result->AssemblyHolder;
    if (assembly != nullptr)
        assembly->AddRef();

    AssemblyHolder = assembly;
    HResult = result->HResult;
    Attempted = result->Attempted;
}

}
#endif
