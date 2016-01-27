// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
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
    m_pRetargetedAssemblyName = NULL;
    m_pIUnknownAssembly = NULL;
}

BindResult::~BindResult()
{
    SAFE_RELEASE(m_pAssemblyName);
    SAFE_RELEASE(m_pRetargetedAssemblyName);
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

AssemblyName *BindResult::GetRetargetedAssemblyName()
{
    return m_pRetargetedAssemblyName;
}

void BindResult::SetRetargetedAssemblyName(AssemblyName *pRetargetedAssemblyName)
{
    SAFE_RELEASE(m_pRetargetedAssemblyName);

    if (pRetargetedAssemblyName)
    {
        pRetargetedAssemblyName->AddRef();
    }

    m_pRetargetedAssemblyName = pRetargetedAssemblyName;
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

BOOL BindResult::GetIsDynamicBind()
{
    return ((m_dwResultFlags & ContextEntry::RESULT_FLAG_IS_DYNAMIC_BIND) != 0);
}

void BindResult::SetIsDynamicBind(BOOL fIsDynamicBind)
{
    if (fIsDynamicBind)
    {
        m_dwResultFlags |= ContextEntry::RESULT_FLAG_IS_DYNAMIC_BIND;
    }
    else
    {
        m_dwResultFlags &= ~ContextEntry::RESULT_FLAG_IS_DYNAMIC_BIND;
    }
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

BOOL BindResult::GetIsSharable()
{
    return ((m_dwResultFlags & ContextEntry::RESULT_FLAG_IS_SHARABLE) != 0);
}

void BindResult::SetIsSharable(BOOL fIsSharable)
{
    if (fIsSharable)
    {
        m_dwResultFlags |= ContextEntry::RESULT_FLAG_IS_SHARABLE;
    }
    else
    {
        m_dwResultFlags &= ~ContextEntry::RESULT_FLAG_IS_SHARABLE;
    }
}

void BindResult::SetResult(ContextEntry *pContextEntry, BOOL fIsContextBound /* = TRUE */)
{
    _ASSERTE(pContextEntry != NULL);

    SetIsDynamicBind(pContextEntry->GetIsDynamicBind());
    SetIsInGAC(pContextEntry->GetIsInGAC());
    SetIsContextBound(fIsContextBound);
    SetIsSharable(pContextEntry->GetIsSharable());
    SAFE_RELEASE(m_pAssemblyName);
    m_pAssemblyName = pContextEntry->GetAssemblyName(TRUE /* fAddRef */);
    m_pIUnknownAssembly = pContextEntry->GetAssembly(TRUE /* fAddRef */);
}

void BindResult::SetResult(Assembly *pAssembly)
{
    _ASSERTE(pAssembly != NULL);

    SetIsDynamicBind(pAssembly->GetIsDynamicBind());
    SetIsInGAC(pAssembly->GetIsInGAC());
    SetIsSharable(pAssembly->GetIsSharable());
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
}

void BindResult::SetNoResult()
{
    m_pAssemblyName = NULL;
}

BOOL BindResult::HaveResult()
{
    return (GetAssemblyName() != NULL);
}

IUnknown *BindResult::ExtractAssembly()
{
    return m_pIUnknownAssembly.Extract();
}

void BindResult::Reset()
{
    SAFE_RELEASE(m_pAssemblyName);
    SAFE_RELEASE(m_pRetargetedAssemblyName);
    m_pIUnknownAssembly = NULL;
    m_dwResultFlags = ContextEntry::RESULT_FLAG_NONE;
}

}

#endif
