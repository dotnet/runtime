// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// 

//


#ifndef __SECURITYDESCRIPTOR_ASSEMBLY_H__
#define __SECURITYDESCRIPTOR_ASSEMBLY_H__

#include "security.h"
#include "securitydescriptor.h"
struct AssemblyLoadSecurity;

class Assembly;
class DomainAssembly;

// Security flags for the objects that store security information
#define CORSEC_ASSERTED             0x000020 // Asseted permission set present on frame
#define CORSEC_DENIED               0x000040 // Denied permission set present on frame
#define CORSEC_REDUCED              0x000080 // Reduced permission set present on frame


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
//
// A Security Descriptor is placed on AppDomain and Assembly (Unmanged) objects.
// AppDomain and Assembly could be from different zones.
// Security Descriptor could also be placed on a native frame.
//
///////////////////////////////////////////////////////////////////////////////

#define MAX_PASSED_DEMANDS 10

//------------------------------------------------------------------
//
//          ASSEMBLY SECURITY DESCRIPTOR
//
//------------------------------------------------------------------

#ifndef DACCESS_COMPILE
void StoreObjectInLazyHandle(LOADERHANDLE& handle, OBJECTREF ref, LoaderAllocator* la);
#endif
class AssemblySecurityDescriptor : public SecurityDescriptorBase<IAssemblySecurityDescriptor>
{
public:
    VPTR_VTABLE_CLASS(AssemblySecurityDescriptor, SecurityDescriptorBase<IAssemblySecurityDescriptor>)

private:
    PsetCacheEntry*   m_arrPassedLinktimeDemands[MAX_PASSED_DEMANDS];
    DWORD   m_dwNumPassedDemands;

    COR_TRUST                                *m_pSignature;            // Contains the publisher, requested permission
    SharedSecurityDescriptor                 *m_pSharedSecDesc;        // Shared state for assemblies loaded into multiple appdomains

#ifdef FEATURE_CAS_POLICY
    LOADERHANDLE m_hRequiredPermissionSet;  // Required Requested Permissions
    LOADERHANDLE m_hOptionalPermissionSet;  // Optional Requested Permissions
    LOADERHANDLE m_hDeniedPermissionSet;    // Denied Permissions

    BOOL                m_fAdditionalEvidence;
    BOOL                m_fIsSignatureLoaded;
    BOOL                m_fAssemblyRequestsComputed;
#endif // FEATURE_CAS_POLICY

    BOOL                m_fMicrosoftPlatform;
    BOOL                m_fAllowSkipVerificationInFullTrust;

#ifndef DACCESS_COMPILE
public:
    virtual SharedSecurityDescriptor *GetSharedSecDesc();

    virtual BOOL CanAssert();
    virtual BOOL HasUnrestrictedUIPermission();
    virtual BOOL IsAllCritical();
    virtual BOOL IsAllSafeCritical();
    virtual BOOL IsAllPublicAreaSafeCritical();
    virtual BOOL IsAllTransparent();
    virtual BOOL IsSystem();
	BOOL QuickIsFullyTrusted();

	BOOL CanSkipVerification();
    virtual BOOL AllowSkipVerificationInFullTrust();

    virtual VOID Resolve();

    virtual void ResolvePolicy(ISharedSecurityDescriptor *pSharedDesc, BOOL fShouldSkipPolicyResolution);

	AssemblySecurityDescriptor(AppDomain *pDomain, DomainAssembly *pAssembly, LoaderAllocator *pLoaderAllocator);

    inline BOOL AlreadyPassedDemand(PsetCacheEntry *pCasDemands);
    inline void TryCachePassedDemand(PsetCacheEntry *pCasDemands);
    Assembly* GetAssembly();
	
#ifndef DACCESS_COMPILE
    virtual void PropagatePermissionSet(OBJECTREF GrantedPermissionSet, OBJECTREF DeniedPermissionSet, DWORD dwSpecialFlags);
#endif	// !DACCESS_COMPILE

#ifdef FEATURE_CAS_POLICY
    virtual HRESULT LoadSignature(COR_TRUST **ppSignature = NULL);
    virtual OBJECTREF GetEvidence();
    DWORD GetZone();

    OBJECTREF GetRequestedPermissionSet(OBJECTREF *pOptionalPermissionSet, OBJECTREF *pDeniedPermissionSet);

    virtual void SetRequestedPermissionSet(OBJECTREF RequiredPermissionSet,
                                          OBJECTREF OptionalPermissionSet,
                                          OBJECTREF DeniedPermissionSet);

#ifndef DACCESS_COMPILE
    virtual void SetAdditionalEvidence(OBJECTREF evidence);
    virtual BOOL HasAdditionalEvidence();
    virtual OBJECTREF GetAdditionalEvidence();
    virtual void SetEvidenceFromPEFile(IPEFileSecurityDescriptor *pPEFileSecDesc);
#endif	// !DACCESS_COMPILE
#endif // FEATURE_CAS_POLICY
	
#ifndef FEATURE_CORECLR 
    virtual BOOL AllowApplicationSpecifiedAppDomainManager();
#endif // !FEATURE_CORECLR

    virtual void CheckAllowAssemblyLoad();

#ifdef FEATURE_CORECLR
    inline BOOL IsMicrosoftPlatform();
#endif // FEATURE_CORECLR

private:
    BOOL CanSkipPolicyResolution();
    OBJECTREF UpgradePEFileEvidenceToAssemblyEvidence(const OBJECTREF& objPEFileEvidence);

    void ResolveWorker();

#ifdef FEATURE_CAS_POLICY
    inline BOOL IsAssemblyRequestsComputed();
    inline BOOL IsSignatureLoaded();
    inline void SetSignatureLoaded();
#endif

#ifdef FEATURE_APTCA
    // If you think you need to call this method, you're probably wrong.  We shouldn't be making any
    // security enforcement decisions based upon this result -- it's strictly for ensuring that we load
    // conditional APTCA assemblies correctly.
    inline BOOL IsConditionalAptca();
#endif // FEATURE_APTCA

#ifdef FEATURE_CORECLR
    inline void SetMicrosoftPlatform();
#endif // FEAUTRE_CORECLR
#endif // #ifndef DACCESS_COMPILE
};


// This really isn't in the SecurityDescriptor hierarchy, per-se. It's attached
// to the unmanaged assembly object and used to store common information when
// the assembly is shared across multiple appdomains.
class SharedSecurityDescriptor : public ISharedSecurityDescriptor
{
private:
    // Unmanaged assembly this descriptor is attached to.
    Assembly           *m_pAssembly;

    // All policy resolution is funnelled through the shared descriptor so we
    // can guarantee everyone's using the same grant/denied sets.
    BOOL                m_fResolved;
    BOOL                m_fFullyTrusted;
    BOOL                m_fCanCallUnmanagedCode;
    BOOL                m_fCanAssert;
    BOOL                m_fMicrosoftPlatform;

public:
    SharedSecurityDescriptor(Assembly *pAssembly);
    virtual ~SharedSecurityDescriptor() {}

    // All policy resolution is funnelled through the shared descriptor so we
    // can guarantee everyone's using the same grant/denied sets.
    virtual void Resolve(IAssemblySecurityDescriptor *pSecDesc = NULL);
	virtual BOOL IsResolved() const;

    // Is this assembly a system assembly?
    virtual BOOL IsSystem();    
    virtual Assembly* GetAssembly();

    inline BOOL IsMicrosoftPlatform();
    BOOL IsFullyTrusted();
    BOOL CanCallUnmanagedCode() const;
    BOOL CanAssert();
};

#include "securitydescriptorassembly.inl"

#endif // #define __SECURITYDESCRIPTOR_ASSEMBLY_H__
