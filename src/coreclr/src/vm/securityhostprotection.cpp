// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// 

//


#include "common.h"
#include "securityattributes.h"
#include "security.h"
#include "eeconfig.h"
#include "corhost.h"

CorHostProtectionManager::CorHostProtectionManager()
{
    CONTRACTL 
    {
        MODE_ANY;
        GC_NOTRIGGER;
        NOTHROW;
    }CONTRACTL_END;

    m_eProtectedCategories = eNoChecks;
    m_fEagerSerializeGrantSet = false;
    m_fFrozen = false;
}

HRESULT CorHostProtectionManager::QueryInterface(REFIID id, void **pInterface)
{
    WRAPPER_NO_CONTRACT;
    STATIC_CONTRACT_SO_TOLERANT;
#ifdef FEATURE_INCLUDE_ALL_INTERFACES
    if (id == IID_ICLRHostProtectionManager)
    {
        *pInterface = GetHostProtectionManager();
        return S_OK;
    }
#endif // FEATURE_INCLUDE_ALL_INTERFACES
    return E_NOINTERFACE;
}

ULONG CorHostProtectionManager::AddRef()
{
    LIMITED_METHOD_CONTRACT;
    return 1;
}

ULONG CorHostProtectionManager::Release()
{
    LIMITED_METHOD_CONTRACT;
    return 1;
}

void CorHostProtectionManager::Freeze()
{
    LIMITED_METHOD_CONTRACT;
    m_fFrozen = true;
}

HRESULT CorHostProtectionManager::SetProtectedCategories(EApiCategories eProtectedCategories)
{
    LIMITED_METHOD_CONTRACT;
    STATIC_CONTRACT_SO_TOLERANT;

    if(m_fFrozen)
        return E_FAIL;
    if((eProtectedCategories | eAll) != eAll)
        return E_FAIL;
    m_eProtectedCategories = eProtectedCategories;
    return S_OK;
}

EApiCategories CorHostProtectionManager::GetProtectedCategories()
{
    WRAPPER_NO_CONTRACT;

    Freeze();
    return m_eProtectedCategories;
}

bool CorHostProtectionManager::GetEagerSerializeGrantSets() const
{
    LIMITED_METHOD_CONTRACT;

    // To provide more context about this flag in the hosting API, this is the case where, 
    // during the unload of an appdomain, we need to serialize a grant set for a shared assembly 
    // that has resolved policy in order to maintain the invariant that the same assembly loaded 
    // into another appdomain created in the future will be granted the same permissions
    // (since the current policy is potentially burned into the jitted code of the shared assembly already).

    return m_fEagerSerializeGrantSet;
}

HRESULT CorHostProtectionManager::SetEagerSerializeGrantSets()
{
    LIMITED_METHOD_CONTRACT;
    STATIC_CONTRACT_SO_TOLERANT;

    m_fEagerSerializeGrantSet = true;
    return S_OK;
}
