// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// 

// 


#ifndef __SECURITYATTRIBUTES_INL__
#define __SECURITYATTRIBUTES_INL__

#include "securityattributes.h"


inline bool SecurityAttributes::ContainsBuiltinCASPermsOnly(CORSEC_ATTRSET* pAttrSet)
{ 
    bool hostProtectiononly;
    return ContainsBuiltinCASPermsOnly(pAttrSet, &hostProtectiononly);
}


inline bool SecurityAttributes::ContainsBuiltinCASPermsOnly(CORSEC_ATTRSET* pAttrSet, bool* pHostProtectionOnly)
{
    DWORD n;
    *pHostProtectionOnly = true; // Assume that it's all HostProtection only
    for(n = 0; n < pAttrSet->dwAttrCount; n++)
    {
        CORSEC_ATTRIBUTE* pAttr = &pAttrSet->pAttrs[n];
        if(!IsBuiltInCASPermissionAttribute(pAttr))
        {
            *pHostProtectionOnly = false;
            return false;
        }
        if (*pHostProtectionOnly && !IsHostProtectionAttribute(pAttr))
        {
            *pHostProtectionOnly = false;
        }
    }

    return true;        
}

#endif // __SECURITYATTRIBUTES_INL__

