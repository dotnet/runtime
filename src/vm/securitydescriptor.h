// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//

//


#ifndef __SECURITYDESCRIPTOR_H__
#define __SECURITYDESCRIPTOR_H__

#include "securityattributes.h"
#include "securitypolicy.h"

class ISecurityDescriptor;
class IPEFileSecurityDescriptor;

// Security flags for the objects that store security information
#define CORSEC_ASSERTED             0x000020 // Asseted permission set present on frame
#define CORSEC_DENIED               0x000040 // Denied permission set present on frame
#define CORSEC_REDUCED              0x000080 // Reduced permission set present on frame

// Inline Functions to support lazy handles - read/write to handle that may not have been created yet 
// SecurityDescriptor and ApplicationSecurityDescriptor currently use these
inline OBJECTREF ObjectFromLazyHandle(LOADERHANDLE handle,  LoaderAllocator* la);

#ifndef DACCESS_COMPILE

inline void StoreObjectInLazyHandle(LOADERHANDLE& handle, OBJECTREF ref, LoaderAllocator* la);


#endif // #ifndef DACCESS_COMPILE


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

///////////////////////////////////////////////////////////////////////////////
//
// SecurityDescriptor is the base class for all security descriptors.
// Extend this class to implement SecurityDescriptors for Assemblies and
// AppDomains.
//
// WARNING : Do not add virtual methods to this class! Doing so results
// in derived classes such as AssemblySecurityDescriptor having two v-table 
// pointers, which the DAC doesn't support. 
//
///////////////////////////////////////////////////////////////////////////////

class SecurityDescriptor
{
protected:

    // The unmanaged DomainAssembly object
    DomainAssembly     *m_pAssem;

    // The PEFile associated with the DomainAssembly
    PEFile      *m_pPEFile;

    // The AppDomain context
    AppDomain*   m_pAppDomain;

    BOOL         m_fSDResolved;

    DWORD        m_dwSpecialFlags;
    LoaderAllocator *m_pLoaderAllocator;

private:
#ifndef CROSSGEN_COMPILE
    LOADERHANDLE m_hGrantedPermissionSet;   // Granted Permission
    LOADERHANDLE m_hGrantDeniedPermissionSet;// Specifically Denied Permissions
#endif // CROSSGEN_COMPILE

public:
    BOOL IsFullyTrusted();
    DWORD GetSpecialFlags() const;

    AppDomain* GetDomain() const;
    BOOL CanCallUnmanagedCode() const;
	

#ifndef DACCESS_COMPILE
    void SetGrantedPermissionSet(OBJECTREF GrantedPermissionSet,
                                        OBJECTREF DeniedPermissionSet,
                                        DWORD dwSpecialFlags);
    OBJECTREF GetGrantedPermissionSet(OBJECTREF* pRefusedPermissions = NULL);
#endif // DACCESS_COMPILE

    BOOL IsResolved() const;

    // Checks for one of the special security flags such as FullTrust or UnmanagedCode
    FORCEINLINE BOOL CheckSpecialFlag (DWORD flags) const;
	
    // Used to locate the assembly
    inline PEFile *GetPEFile() const;

protected:
    //--------------------
    // Constructor
    //--------------------
#ifndef DACCESS_COMPILE
    inline SecurityDescriptor(AppDomain *pAppDomain, DomainAssembly *pAssembly, PEFile* pPEFile, LoaderAllocator *pLoaderAllocator);    
#ifdef FEATURE_PAL
    SecurityDescriptor() {}
#endif // FEATURE_PAL
#endif // !DACCESS_COMPILE
};

template<typename IT>
class SecurityDescriptorBase : public IT, public SecurityDescriptor
{
public:
    VPTR_ABSTRACT_VTABLE_CLASS(SecurityDescriptorBase, IT) // needed for the DAC

    inline SecurityDescriptorBase(AppDomain *pAppDomain, DomainAssembly *pAssembly, PEFile* pPEFile, LoaderAllocator *pLoaderAllocator);

public:
    virtual BOOL IsFullyTrusted() { return SecurityDescriptor::IsFullyTrusted(); }
    virtual BOOL CanCallUnmanagedCode() const { return SecurityDescriptor::CanCallUnmanagedCode(); }
    virtual DWORD GetSpecialFlags() const { return SecurityDescriptor::GetSpecialFlags(); }
    
    virtual AppDomain* GetDomain() const { return SecurityDescriptor::GetDomain(); }

    virtual BOOL IsResolved() const { return SecurityDescriptor::IsResolved(); }


#ifndef DACCESS_COMPILE
    virtual OBJECTREF GetGrantedPermissionSet(OBJECTREF* RefusedPermissions = NULL) { return SecurityDescriptor::GetGrantedPermissionSet(RefusedPermissions); }
#endif
};


#include "securitydescriptor.inl"

#endif // #define __SECURITYDESCRIPTOR_H__

