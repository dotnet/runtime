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

	

    virtual void CheckAllowAssemblyLoad();

private:
    BOOL CanSkipPolicyResolution();
    OBJECTREF UpgradePEFileEvidenceToAssemblyEvidence(const OBJECTREF& objPEFileEvidence);

    void ResolveWorker();



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

    BOOL IsFullyTrusted();
    BOOL CanCallUnmanagedCode() const;
    BOOL CanAssert();
};

#include "securitydescriptorassembly.inl"

#endif // #define __SECURITYDESCRIPTOR_ASSEMBLY_H__
