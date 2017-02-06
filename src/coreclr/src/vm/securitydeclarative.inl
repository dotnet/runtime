// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//

// 


#ifndef __SECURITYDECLARATIVE_INL__
#define __SECURITYDECLARATIVE_INL__

#include "security.h"

inline LinktimeCheckReason operator|(LinktimeCheckReason lhs, LinktimeCheckReason rhs)
{
    LIMITED_METHOD_CONTRACT;
    return static_cast<LinktimeCheckReason>(static_cast<DWORD>(lhs) | static_cast<DWORD>(rhs));
}

inline LinktimeCheckReason operator|=(LinktimeCheckReason &lhs, LinktimeCheckReason rhs)
{
    LIMITED_METHOD_CONTRACT;
    lhs = lhs | rhs;
    return lhs;
}

inline LinktimeCheckReason operator&(LinktimeCheckReason lhs, LinktimeCheckReason rhs)
{
    LIMITED_METHOD_CONTRACT;
    return static_cast<LinktimeCheckReason>(static_cast<DWORD>(lhs) & static_cast<DWORD>(rhs));
}


inline LinktimeCheckReason operator&=(LinktimeCheckReason &lhs, LinktimeCheckReason rhs)
{
    LIMITED_METHOD_CONTRACT;
    lhs = lhs & rhs;
    return lhs;
}

inline BOOL SecurityDeclarative::FullTrustCheckForLinkOrInheritanceDemand(Assembly *pAssembly)
{
    WRAPPER_NO_CONTRACT;
#ifndef DACCESS_COMPILE
    IAssemblySecurityDescriptor* pSecDesc = pAssembly->GetSecurityDescriptor();
    if (pSecDesc->IsSystem())
        return TRUE;
  
    if (pSecDesc->IsFullyTrusted())
        return TRUE;
#endif  
    return FALSE;
      
}

inline BOOL SecurityDeclarative::MethodIsVisibleOutsideItsAssembly(DWORD dwMethodAttr)
{
    LIMITED_METHOD_CONTRACT;
    return ( IsMdPublic(dwMethodAttr)    || 
                IsMdFamORAssem(dwMethodAttr)||
                IsMdFamily(dwMethodAttr) );
}

inline BOOL SecurityDeclarative::MethodIsVisibleOutsideItsAssembly(
            MethodDesc * pMD)
{
    LIMITED_METHOD_CONTRACT;

    MethodTable * pMT = pMD->GetMethodTable();

    if (!ClassIsVisibleOutsideItsAssembly(pMT->GetAttrClass(), pMT->IsGlobalClass()))
        return FALSE;
    
    return MethodIsVisibleOutsideItsAssembly(pMD->GetAttrs());
}

inline BOOL SecurityDeclarative::MethodIsVisibleOutsideItsAssembly(DWORD dwMethodAttr, DWORD dwClassAttr, BOOL fIsGlobalClass)
{
    LIMITED_METHOD_CONTRACT;

    if (!ClassIsVisibleOutsideItsAssembly(dwClassAttr, fIsGlobalClass))
        return FALSE;

    return MethodIsVisibleOutsideItsAssembly(dwMethodAttr);
}

inline BOOL SecurityDeclarative::ClassIsVisibleOutsideItsAssembly(DWORD dwClassAttr, BOOL fIsGlobalClass)
{
    LIMITED_METHOD_CONTRACT;

    if (fIsGlobalClass)
    {
        return TRUE;
    }

    return ( IsTdPublic(dwClassAttr)      || 
                IsTdNestedPublic(dwClassAttr)||
                IsTdNestedFamily(dwClassAttr)||
                IsTdNestedFamORAssem(dwClassAttr));
}

#ifndef DACCESS_COMPILE
inline void SecurityDeclarative::DoDeclarativeSecurityAtStackWalk(MethodDesc* pFunc, AppDomain* pAppDomain, OBJECTREF* pFrameObjectSlot)
{
    CONTRACTL {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
    } CONTRACTL_END;
    

    BOOL hasDeclarativeStackModifier = (pFunc->IsInterceptedForDeclSecurity() && !pFunc->IsInterceptedForDeclSecurityCASDemandsOnly());
    if (hasDeclarativeStackModifier)
    {

        _ASSERTE(pFrameObjectSlot != NULL);
        if (*pFrameObjectSlot == NULL || !( ((FRAMESECDESCREF)(*pFrameObjectSlot))->IsDeclSecComputed()) )
        {
            // Populate the FSD with declarative assert/deny/PO
            SecurityDeclarative::DoDeclarativeStackModifiers(pFunc, pAppDomain, pFrameObjectSlot);
        }
    }
}
#endif



#endif // __SECURITYDECLARATIVE_INL__
