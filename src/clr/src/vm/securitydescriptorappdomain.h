//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//
// 

// 


#ifndef __SECURITYDESCRIPTOR_APPDOMAIN_H__
#define __SECURITYDESCRIPTOR_APPDOMAIN_H__
#include "security.h"
#include "securitydescriptor.h"
#include "securitymeta.h"

///////////////////////////////////////////////////////////////////////////////
//
//      [SecurityDescriptor]
//      |
//      +----[PEFileSecurityDescriptor]
//      |
//      +----[ApplicationSecurityDescriptor]
//      |
//      +----[AssemblySecurityDescriptor]
//
//      [SharedSecurityDescriptor]
//
///////////////////////////////////////////////////////////////////////////////

//------------------------------------------------------------------
//
//          APPDOMAIN SECURITY DESCRIPTOR
//
//------------------------------------------------------------------

class ApplicationSecurityDescriptor : public SecurityDescriptorBase<IApplicationSecurityDescriptor>
{
public:
#ifndef FEATURE_PAL
    VPTR_VTABLE_CLASS(ApplicationSecurityDescriptor, SecurityDescriptorBase<IApplicationSecurityDescriptor>)
#endif

private:
    // Dependency in managed : System.Security.HostSecurityManager.cs
    enum HostSecurityManagerFlags
    {
        // Flags to control which HostSecurityManager features are provided by the host
        HOST_NONE                   = 0x0000,
        HOST_APP_DOMAIN_EVIDENCE    = 0x0001,
        HOST_POLICY_LEVEL           = 0x0002,
        HOST_ASM_EVIDENCE           = 0x0004,
        HOST_DAT                    = 0x0008,
        HOST_RESOLVE_POLICY         = 0x0010
    };

#ifdef FEATURE_PLS
    // Intersection of granted/denied permissions of all assemblies in domain
    LOADERHANDLE m_hDomainPermissionListSet; 
#endif // FEATURE_PLS

    // The bits represent the status of security checks on some specific permissions within this domain
    Volatile<DWORD> m_dwDomainWideSpecialFlags;
    // m_dwDomainWideSpecialFlags bit map
    // Bit 0 = Unmanaged Code access permission. Accessed via SECURITY_UNMANAGED_CODE
    // Bit 1 = Skip verification permission. SECURITY_SKIP_VER
    // Bit 2 = Permission to Reflect over types. REFLECTION_TYPE_INFO
    // Bit 3 = Permission to Assert. SECURITY_ASSERT
    // Bit 4 = Permission to invoke methods. REFLECTION_MEMBER_ACCESS
    // Bit 7 = PermissionSet, fulltrust SECURITY_FULL_TRUST
    // Bit 9 = UIPermission (unrestricted)

    BOOL m_fIsInitializationInProgress; // appdomain is in the initialization stage and is considered FullTrust by the security system.
    BOOL m_fIsDefaultAppdomain;         // appdomain is the default appdomain, or created by the default appdomain without an explicit evidence
    BOOL m_fIsDefaultAppdomainEvidence; // Evidence for this AD is the same as the Default AD. 
                                        // m_ifIsDefaultAppDomain is TRUE => m_fIsDefaultAppdomainEvidence is TRUE
                                        // m_fIsDefaultAppdomainEvidence can be TRUE when m_fIsDefaultAppdomain is FALSE if a homogeneous AD was
                                        // created without evidence (non-null PermissionSet though). 
                                        // m_fIsDefaultAppdomainEvidence and m_fIsDefaultAppdomain are both FALSE when an explicit evidence
                                        // exists on the AppDomain. (In the managed world: AppDomain._SecurityIdentity != null)
    BOOL m_fHomogeneous;                // This AppDomain has an ApplicationTrust
    BOOL m_fRuntimeSuppliedHomogenousGrantSet; // This AppDomain is homogenous only because the v4 CLR defaults to creating homogenous domains, and would not have been homogenous in v2
#ifdef FEATURE_CAS_POLICY
    BOOL m_fLegacyCasPolicy;            // This AppDomain is using legacy CAS policy
#endif // FEATURE_CAS_POLICY
    DWORD m_dwHostSecurityManagerFlags; // Flags indicating what decisions the host wants to participate in.
    BOOL m_fContainsAnyRefusedPermissions;

    BOOL m_fIsPreResolved;              // Have we done a pre-resolve on this domain yet
    BOOL m_fPreResolutionFullTrust;     // Was the domain pre-resolved to be full trust
    BOOL m_fPreResolutionHomogeneous;   // Was the domain pre-resolved to be homogenous
    
#ifdef FEATURE_APTCA
    ConditionalAptcaCache*   m_pConditionalAptcaCache;    // Cache of known conditional APTCA assemblies in this domain
#endif // FEATURE_APTCA

#ifndef DACCESS_COMPILE
public:
    //--------------------
    // Constructor
    //--------------------
    inline ApplicationSecurityDescriptor(AppDomain *pAppDomain);

    //--------------------
    // Destructor
    //--------------------
#ifdef FEATURE_APTCA  //  The destructor only deletes the ConditionalAptcaCache
    inline ~ApplicationSecurityDescriptor();
#endif // FEATURE_APTCA

public:
    // Indicates whether the initialization phase is in progress.
    virtual BOOL IsInitializationInProgress();
    inline void ResetInitializationInProgress();

    // The AppDomain is considered a default one (FT) if the property is
    // set and it's not a homogeneous AppDomain (ClickOnce case for example).
    virtual BOOL IsDefaultAppDomain() const;
    inline void SetDefaultAppDomain();

    virtual BOOL IsDefaultAppDomainEvidence();
    inline void SetDefaultAppDomainEvidence();

    virtual VOID Resolve();

    void ResolveWorker();

    virtual void FinishInitialization();

    virtual void PreResolve(BOOL *pfIsFullyTrusted, BOOL *pfIsHomogeneous);

    virtual void SetHostSecurityManagerFlags(DWORD dwFlags);
    virtual void SetPolicyLevelFlag();

    inline void SetHomogeneousFlag(BOOL fRuntimeSuppliedHomogenousGrantSet);
    virtual BOOL IsHomogeneous() const;

#ifdef FEATURE_CAS_POLICY
    virtual BOOL IsLegacyCasPolicyEnabled();
    virtual void SetLegacyCasPolicyEnabled();
#endif // FEATURE_CAS_POLICY
    
    virtual BOOL ContainsAnyRefusedPermissions();

    // Should the HSM be consulted for security decisions in this AppDomain.
    virtual BOOL CallHostSecurityManager();

#ifdef FEATURE_CAS_POLICY
    // Does the domain's HSM need to be consulted for assemblies loaded into the domain
    inline BOOL CallHostSecurityManagerForAssemblies();
#endif // FEATURE_CAS_POLICY

    // Initialize the PLS on the AppDomain.
    void InitializePLS();

    // Called everytime an AssemblySecurityDescriptor is resolved.
    void AddNewSecDescToPLS(AssemblySecurityDescriptor *pNewSecDescriptor);

#ifdef FEATURE_PLS
    // Check the demand against the PLS in this AppDomain
    BOOL CheckPLS (OBJECTREF* orDemand, DWORD dwDemandSpecialFlags, BOOL fDemandSet);
#endif // FEATURE_PLS

    // Checks for one of the special domain wide flags 
    // such as if we are currently in a "fully trusted" environment
    // or if unmanaged code access is allowed at this time
    inline BOOL CheckDomainWideSpecialFlag(DWORD flags) const;
    virtual DWORD GetDomainWideSpecialFlag() const;

#ifdef FEATURE_CAS_POLICY
    virtual OBJECTREF GetEvidence();
    DWORD GetZone();

    virtual BOOL AllowsLoadsFromRemoteSources();
#endif // FEATURE_CAS_POLICY

    virtual BOOL DomainMayContainPartialTrustCode();

    BOOL QuickIsFullyTrusted();

#ifdef FEATURE_APTCA
    virtual ConditionalAptcaCache *GetConditionalAptcaCache();
    virtual void SetCanonicalConditionalAptcaList(LPCWSTR wszCanonicalConditionalAptcaList);
#endif // FEATURE_APTCA
#endif // #ifndef DACCESS_COMPILE
};

#include "securitydescriptorappdomain.inl"

#endif // #define __SECURITYDESCRIPTOR_APPDOMAIN_H__
