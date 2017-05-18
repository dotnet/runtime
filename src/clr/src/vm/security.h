// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// 

#ifndef __security_h__
#define __security_h__

//
// Stubbed out implementation of security subsystem
// TODO: Eliminate this file
//

enum SecurityStackWalkType
{
    SSWT_DECLARATIVE_DEMAND = 1,
    SSWT_IMPERATIVE_DEMAND = 2,
    SSWT_DEMAND_FROM_NATIVE = 3,
    SSWT_IMPERATIVE_ASSERT = 4,
    SSWT_DENY_OR_PERMITONLY = 5,
    SSWT_LATEBOUND_LINKDEMAND = 6,
    SSWT_COUNT_OVERRIDES = 7,
    SSWT_GET_ZONE_AND_URL = 8,
};

// special flags
#define SECURITY_UNMANAGED_CODE                 0
#define SECURITY_SKIP_VER                       1
#define REFLECTION_TYPE_INFO                    2
#define SECURITY_ASSERT                         3
#define REFLECTION_MEMBER_ACCESS                4
#define SECURITY_SERIALIZATION                  5
#define REFLECTION_RESTRICTED_MEMBER_ACCESS     6
#define SECURITY_FULL_TRUST                     7
#define SECURITY_BINDING_REDIRECTS              8

// Ultimately this will become the only interface through
// which the VM will access security code.

namespace Security
{
    inline BOOL IsTransparencyEnforcementEnabled() { return false; }

    inline BOOL CanCallUnmanagedCode(Module *pModule) { return true; }

#ifndef DACCESS_COMPILE
    inline BOOL CanTailCall(MethodDesc* pMD) { return true; }
    inline BOOL CanHaveRVA(Assembly * pAssembly) { return true; }
    inline BOOL CanAccessNonVerifiableExplicitField(MethodDesc* pMD) { return true; }
    inline BOOL CanSkipVerification(MethodDesc * pMethod) { return true; }
#endif

    inline BOOL CanSkipVerification(DomainAssembly * pAssembly) { return true; }

    // ----------------------------------------
    // SecurityAttributes
    // ----------------------------------------

    void CopyByteArrayToEncoding(IN U1ARRAYREF* pArray, OUT PBYTE* pbData, OUT DWORD* cbData);
    void CopyEncodingToByteArray(IN PBYTE   pbData, IN DWORD   cbData, IN OBJECTREF* pArray);    

    inline void SpecialDemand(SecurityStackWalkType eType, DWORD whatPermission) { }

    // Transparency checks
    inline BOOL IsMethodTransparent(MethodDesc * pMD) { return false; }
    inline BOOL IsMethodCritical(MethodDesc * pMD) { return true; }
    inline BOOL IsMethodSafeCritical(MethodDesc * pMD) { return false; }

    inline BOOL IsTypeCritical(MethodTable *pMT) { return true; }
    inline BOOL IsTypeSafeCritical(MethodTable *pMT) { return false; }
    inline BOOL IsTypeTransparent(MethodTable * pMT) { return false; }
    inline BOOL IsTypeAllTransparent(MethodTable * pMT) { return false; }

    inline BOOL IsFieldTransparent(FieldDesc * pFD) { return false; }
    inline BOOL IsFieldCritical(FieldDesc * pFD) { return true; }
    inline BOOL IsFieldSafeCritical(FieldDesc * pFD) { return false; }

    inline BOOL IsTokenTransparent(Module* pModule, mdToken token) { return false; }

    inline BOOL CheckCriticalAccess(AccessCheckContext* pContext,
        MethodDesc* pOptionalTargetMethod = NULL,
        FieldDesc* pOptionalTargetField = NULL,
        MethodTable * pOptionalTargetType = NULL)
    {
        return true;
    }

    inline void CheckLinkDemandAgainstAppDomain(MethodDesc *pMD)
    {
    }
};

#endif
