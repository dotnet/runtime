// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//

//


#ifndef __SECURITYDESCRIPTOR_INL__
#define __SECURITYDESCRIPTOR_INL__

// Inline Functions to support lazy handles - read/write to handle that may not have been created yet 
// SecurityDescriptor and ApplicationSecurityDescriptor currently use these
inline OBJECTREF ObjectFromLazyHandle(LOADERHANDLE handle, LoaderAllocator *pLoaderAllocator)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        SO_TOLERANT;
        MODE_COOPERATIVE;
    }
    CONTRACTL_END;

    if (handle != NULL)
    {
        return pLoaderAllocator->GetHandleValue(handle);
    }
    else
    {
        return NULL;
    }
}

#ifndef DACCESS_COMPILE

inline SecurityDescriptor::SecurityDescriptor(AppDomain *pAppDomain,
                                              DomainAssembly *pAssembly,
                                              PEFile* pPEFile,
                                              LoaderAllocator *pLoaderAllocator) :
    m_pAssem(pAssembly),
    m_pPEFile(pPEFile),
    m_pAppDomain(pAppDomain),
    m_fSDResolved(FALSE),
    m_dwSpecialFlags(0),
    m_pLoaderAllocator(pLoaderAllocator)
#ifndef CROSSGEN_COMPILE
    , m_hGrantedPermissionSet(NULL),
    m_hGrantDeniedPermissionSet(NULL)
#endif // CROSSGEN_COMPILE    
{
    LIMITED_METHOD_CONTRACT;
}
#endif // !DACCESS_COMPILE


// Checks for one of the special security flags such as FullTrust or UnmanagedCode
FORCEINLINE BOOL SecurityDescriptor::CheckSpecialFlag (DWORD flags) const
{ 
    LIMITED_METHOD_CONTRACT;
    SUPPORTS_DAC;

    return (m_dwSpecialFlags & flags);
}

inline PEFile *SecurityDescriptor::GetPEFile() const
{
    LIMITED_METHOD_CONTRACT;
    return m_pPEFile;
}

#ifndef DACCESS_COMPILE
template<typename IT>
inline SecurityDescriptorBase<IT>::SecurityDescriptorBase(AppDomain *pAppDomain,
                                                      DomainAssembly *pAssembly,
                                                      PEFile* pPEFile,
                                                      LoaderAllocator *pLoaderAllocator) :
        SecurityDescriptor(pAppDomain, pAssembly, pPEFile, pLoaderAllocator)
{
}
#endif // !DACCESS_COMPILE


#endif // #define __SECURITYDESCRIPTOR_INL__
