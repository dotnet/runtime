// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// 

// 


#include "common.h"

#include "security.h"
#include "eventtrace.h"

///////////////////////////////////////////////////////////////////////////////
//
//  [SecurityDescriptor]
//  |
//  |
//  +----[PEFileSecurityDescriptor]
//
///////////////////////////////////////////////////////////////////////////////

BOOL SecurityDescriptor::CanCallUnmanagedCode () const
{
    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        PRECONDITION(IsResolved() || m_pAppDomain->GetSecurityDescriptor()->IsInitializationInProgress());
    } CONTRACTL_END;

    return CheckSpecialFlag(1 << SECURITY_UNMANAGED_CODE);
}

#ifndef DACCESS_COMPILE

OBJECTREF SecurityDescriptor::GetGrantedPermissionSet(OBJECTREF* pRefusedPermissions)
{
    CONTRACTL {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        PRECONDITION(IsResolved() || m_pAppDomain->GetSecurityDescriptor()->IsInitializationInProgress());
        INJECT_FAULT(COMPlusThrowOM(););
    } CONTRACTL_END;

#ifndef CROSSGEN_COMPILE
    if (pRefusedPermissions)
        *pRefusedPermissions = ObjectFromLazyHandle(m_hGrantDeniedPermissionSet, m_pLoaderAllocator);
    return ObjectFromLazyHandle(m_hGrantedPermissionSet, m_pLoaderAllocator);
#else
    return NULL;
#endif
}

//
// Returns TRUE if the given zone has the given special permission.
//

#endif // DACCESS_COMPILE


//
// This method will return TRUE if this object is fully trusted.
//

BOOL SecurityDescriptor::IsFullyTrusted ()
{
    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        SUPPORTS_DAC;
        SO_TOLERANT;
        PRECONDITION(IsResolved() || m_pAppDomain->GetSecurityDescriptor()->IsInitializationInProgress());
    } CONTRACTL_END;

    return CheckSpecialFlag(1 << SECURITY_FULL_TRUST);
}

BOOL SecurityDescriptor::IsResolved() const
{
    LIMITED_METHOD_CONTRACT;
    return m_fSDResolved;
}

DWORD SecurityDescriptor::GetSpecialFlags() const
{
    LIMITED_METHOD_CONTRACT;
    return m_dwSpecialFlags;
}

#ifndef DACCESS_COMPILE
void SecurityDescriptor::SetGrantedPermissionSet(OBJECTREF GrantedPermissionSet,
                                                        OBJECTREF DeniedPermissionSet,
                                                        DWORD dwSpecialFlags)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END;

#ifndef CROSSGEN_COMPILE
    GCPROTECT_BEGIN(DeniedPermissionSet);
    StoreObjectInLazyHandle(m_hGrantedPermissionSet, GrantedPermissionSet, m_pLoaderAllocator);
    StoreObjectInLazyHandle(m_hGrantDeniedPermissionSet, DeniedPermissionSet, m_pLoaderAllocator);
    GCPROTECT_END();
#endif

    if (dwSpecialFlags & (1 << SECURITY_FULL_TRUST))
    {
        m_dwSpecialFlags = 0xFFFFFFFF; // Fulltrust means that all possible quick checks should succeed, so we set all flags
    }
    else
    {
        m_dwSpecialFlags = dwSpecialFlags;
    }

    m_fSDResolved = TRUE;
}


#endif // !DACCESS_COMPILE

AppDomain* SecurityDescriptor::GetDomain() const
{
    LIMITED_METHOD_CONTRACT;
    return m_pAppDomain;
}

#ifndef DACCESS_COMPILE



#endif // !DACCESS_COMPILE
