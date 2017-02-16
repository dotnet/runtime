// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// 


// 


#ifndef __SECURITYSTACKWALK_H__
#define __SECURITYSTACKWALK_H__

#include "common.h"

#include "object.h"
#include "util.hpp"
#include "fcall.h"
#include "perfcounters.h"
#include "security.h"
#include "holder.h"

class ApplicationSecurityDescriptor;
class DemandStackWalk;
class CountOverridesStackWalk;
class AssertStackWalk;
struct TokenDeclActionInfo;

//-----------------------------------------------------------
// SecurityStackWalk implements all the native methods
// for the managed class System.Security.CodeAccessSecurityEngine.
//-----------------------------------------------------------
class SecurityStackWalk
{
protected:

    SecurityStackWalkType m_eStackWalkType;
    DWORD m_dwFlags;

public:
    struct ObjectCache
    {
        struct gc 
        {
            OBJECTREF object1;
            OBJECTREF object2;    
        }
        m_sGC;
        AppDomain* m_pOriginalDomain;

#ifndef DACCESS_COMPILE
        OBJECTREF GetObjects(AppDomain *pDomain, OBJECTREF *porObject2)
        {
            _ASSERTE(pDomain == ::GetAppDomain());
            _ASSERTE(m_pOriginalDomain == ::GetAppDomain());
            *porObject2 = m_sGC.object2;
            return m_sGC.object1;
        };
        OBJECTREF GetObject(AppDomain *pDomain)
        {
            LIMITED_METHOD_CONTRACT;
            _ASSERTE(pDomain == ::GetAppDomain());
            _ASSERTE(m_pOriginalDomain == ::GetAppDomain());
            return m_sGC.object1;
        };
       void SetObject(OBJECTREF orObject)
        {
            LIMITED_METHOD_CONTRACT;
            m_pOriginalDomain = ::GetAppDomain();
            m_sGC.object1 = orObject;
        }

        // Set the original values of both cached objects.
        void SetObjects(OBJECTREF orObject1, OBJECTREF orObject2)
        {
            LIMITED_METHOD_CONTRACT;
            m_pOriginalDomain = ::GetAppDomain();
            m_sGC.object1 = orObject1;
            m_sGC.object2 = orObject2;
        }        

        void UpdateObject(AppDomain *pDomain, OBJECTREF orObject)
        {
            LIMITED_METHOD_CONTRACT;        
            _ASSERTE(pDomain == ::GetAppDomain());
            _ASSERTE(m_pOriginalDomain == ::GetAppDomain());
            m_sGC.object1 = orObject;
        } 
#endif //!DACCESS_COMPILE
        ObjectCache()
        {
            m_pOriginalDomain = NULL;
            ZeroMemory(&m_sGC,sizeof(m_sGC));
        }
    
    } m_objects;

    SecurityStackWalk(SecurityStackWalkType eType, DWORD flags)
    {
        LIMITED_METHOD_CONTRACT;
        m_eStackWalkType = eType;
        m_dwFlags = flags;
    }

    // ----------------------------------------------------
    // FCalls
    // ----------------------------------------------------

    // FCall wrapper for CheckInternal
    static FCDECL3(void, Check, Object* permOrPermSetUNSAFE, StackCrawlMark* stackMark, CLR_BOOL isPermSet);
    static void CheckFramed(Object* permOrPermSetUNSAFE, StackCrawlMark* stackMark, CLR_BOOL isPermSet);

    // FCALL wrapper for quickcheckforalldemands
    static FCDECL0(FC_BOOL_RET, FCallQuickCheckForAllDemands);
    static FCDECL0(FC_BOOL_RET, FCallAllDomainsHomogeneousWithNoStackModifiers);

    
    static FCDECL3(void, GetZoneAndOrigin, Object* pZoneListUNSAFE, Object* pOriginListUNSAFE, StackCrawlMark* stackMark);

    // Do an imperative assert.  (Check the for the permission and return the SecurityObject for the first frame)
    static FCDECL4(Object*, CheckNReturnSO, Object* permTokenUNSAFE, Object* permUNSAFE, StackCrawlMark* stackMark, INT32 create);


    // Do a demand for a special permission type
    static FCDECL2(void, FcallSpecialDemand, DWORD whatPermission, StackCrawlMark* stackMark);

    // ----------------------------------------------------
    // Checks
    // ----------------------------------------------------

    // Methods for checking grant and refused sets
  
public:
    void CheckPermissionAgainstGrants(OBJECTREF refCS, OBJECTREF refGrants, OBJECTREF refRefused, AppDomain *pDomain, MethodDesc* pMethod, Assembly* pAssembly);

protected:
    void CheckSetAgainstGrants(OBJECTREF refCS, OBJECTREF refGrants, OBJECTREF refRefused, AppDomain *pDomain, MethodDesc* pMethod, Assembly* pAssembly);

    void GetZoneAndOriginGrants(OBJECTREF refCS, OBJECTREF refGrants, OBJECTREF refRefused, AppDomain *pDomain, MethodDesc* pMethod, Assembly* pAssembly);

    // Methods for checking stack modifiers
    BOOL CheckPermissionAgainstFrameData(OBJECTREF refFrameData, AppDomain* pDomain, MethodDesc* pMethod);
    BOOL CheckSetAgainstFrameData(OBJECTREF refFrameData, AppDomain* pDomain, MethodDesc* pMethod);
    
public:
    // ----------------------------------------------------
    // CAS Actions
    // ----------------------------------------------------

    // Native version of CodeAccessPermission.Demand()
    //   Callers:
    //     <Currently unused>
    static void Demand(SecurityStackWalkType eType, OBJECTREF demand);

    // Demand all of the permissions granted to an assembly, with the exception of any identity permissions
    static void DemandGrantSet(AssemblySecurityDescriptor *psdAssembly);

    // Native version of PermissionSet.Demand()
    //   Callers:
    //     CanAccess (ReflectionInvocation)
    //     ReflectionSerialization::GetSafeUninitializedObject
    static void DemandSet(SecurityStackWalkType eType, OBJECTREF demand);

    // Native version of PermissionSet.Demand() that delays instantiating the PermissionSet object
    //   Callers:
    //     InvokeDeclarativeActions
    static void DemandSet(SecurityStackWalkType eType, PsetCacheEntry *pPCE, DWORD dwAction);


    static void ReflectionTargetDemand(DWORD dwPermission, AssemblySecurityDescriptor *psdTarget);

    static void ReflectionTargetDemand(DWORD dwPermission,
                                       AssemblySecurityDescriptor *psdTarget,
                                       DynamicResolver * pAccessContext);

    // Optimized demand for a well-known permission
    //   Callers:
    //     SecurityDeclarative::DoDeclarativeActions
    //     Security::CheckLinkDemandAgainstAppDomain
    //     TryDemand (ReflectionInvocation)
    //     CanAccess (ReflectionInvocation)
    //     ReflectionInvocation::CanValueSpecialCast
    //     RuntimeTypeHandle::CreateInstance
    //     RuntimeMethodHandle::InvokeMethod_Internal
    //     InvokeArrayConstructor (ReflectionInvocation)
    //     ReflectionInvocation::InvokeDispMethod
    //     COMArrayInfo::CreateInstance
    //     COMArrayInfo::CreateInstanceEx
    //     COMDelegate::BindToMethodName
    //     InvokeUtil::CheckArg
    //     InvokeUtil::ValidField
    //     RefSecContext::CallerHasPerm
    //     MngStdItfBase::ForwardCallToManagedView
    //     ObjectClone::Clone
    static void SpecialDemand(SecurityStackWalkType eType, DWORD whatPermission, StackCrawlMark* stackMark = NULL);

    // ----------------------------------------------------
    // Compressed Stack
    // ----------------------------------------------------
public:

#ifndef DACCESS_COMPILE
    FORCEINLINE static BOOL HasFlagsOrFullyTrustedIgnoreMode (DWORD flags);   
    FORCEINLINE static BOOL HasFlagsOrFullyTrusted (DWORD flags);     
#endif // #ifndef DACCESS_COMPILE

public:
    // Perf Counters
    FORCEINLINE static VOID IncrementSecurityPerfCounter()
    {
        CONTRACTL {
            MODE_ANY;
            GC_NOTRIGGER;
            NOTHROW;
            SO_TOLERANT;
        } CONTRACTL_END;
        COUNTER_ONLY(GetPerfCounters().m_Security.cTotalRTChecks++);
    }
    
    // ----------------------------------------------------
    // Misc
    // ----------------------------------------------------
    static bool IsSpecialRunFrame(MethodDesc *pMeth);

    static BOOL SkipAndFindFunctionInfo(INT32, MethodDesc**, OBJECTREF**, AppDomain **ppAppDomain = NULL);
    static BOOL SkipAndFindFunctionInfo(StackCrawlMark*, MethodDesc**, OBJECTREF**, AppDomain **ppAppDomain = NULL);

    // Check the provided demand set against the provided grant/refused set
    static void CheckSetHelper(OBJECTREF *prefDemand,
                               OBJECTREF *prefGrant,
                               OBJECTREF *prefDenied,
                               AppDomain *pGrantDomain,
                               MethodDesc *pMethod,
                               OBJECTREF *pAssembly,
                               CorDeclSecurity action);
    
    // Check for Link/Inheritance CAS permissions
    static void LinkOrInheritanceCheck(IAssemblySecurityDescriptor *pSecDesc, OBJECTREF refDemands, Assembly* pAssembly, CorDeclSecurity action);

private:
    FORCEINLINE static BOOL QuickCheckForAllDemands(DWORD flags);

    // Tries to avoid unnecessary demands
    static BOOL PreCheck(OBJECTREF* orDemand, BOOL fDemandSet = FALSE);
    static DWORD GetPermissionSpecialFlags (OBJECTREF* orDemand);

    // Does a demand for a CodeAccessPermission : First does PreCheck. If PreCheck fails then calls Check_StackWalk 
    static void Check_PLS_SW(BOOL isPermSet, SecurityStackWalkType eType, OBJECTREF* permOrPermSet, StackCrawlMark* stackMark);

    // Calls into Check_PLS_SW after GC protecting "perm "
    static void Check_PLS_SW_GC(BOOL isPermSet, SecurityStackWalkType eType, OBJECTREF permOrPermSet, StackCrawlMark* stackMark);

    // Walks the stack for a CodeAccessPermission demand (assumes PreCheck was already called)
    static void Check_StackWalk(SecurityStackWalkType eType, OBJECTREF* pPerm, StackCrawlMark* stackMark, BOOL isPermSet);

    // Walk the stack and count all the frame descriptors with an Assert, Deny, or PermitOnly
    static VOID UpdateOverridesCount();
};


#endif /* __SECURITYSTACKWALK_H__ */

