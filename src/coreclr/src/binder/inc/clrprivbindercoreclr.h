//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//


#ifndef __CLR_PRIV_BINDER_CORECLR_H__
#define __CLR_PRIV_BINDER_CORECLR_H__

#include "coreclrbindercommon.h"
#include "applicationcontext.hpp"

namespace BINDER_SPACE
{
    class AssemblyIdentityUTF8;
};

class CLRPrivBinderCoreCLR : public IUnknownCommon<ICLRPrivBinder>
{
public:

    //=========================================================================
    // ICLRPrivBinder functions
    //-------------------------------------------------------------------------
    STDMETHOD(BindAssemblyByName)( 
            /* [in] */ IAssemblyName *pIAssemblyName,
            /* [retval][out] */ ICLRPrivAssembly **ppAssembly);
        
    STDMETHOD(VerifyBind)( 
            /* [in] */ IAssemblyName *pIAssemblyName,
            /* [in] */ ICLRPrivAssembly *pAssembly,
            /* [in] */ ICLRPrivAssemblyInfo *pAssemblyInfo);

    STDMETHOD(GetBinderFlags)( 
            /* [retval][out] */ DWORD *pBinderFlags);
         
    STDMETHOD(GetBinderID)( 
            /* [retval][out] */ UINT_PTR *pBinderId);
         
    STDMETHOD(FindAssemblyBySpec)( 
            /* [in] */ LPVOID pvAppDomain,
            /* [in] */ LPVOID pvAssemblySpec,
            /* [out] */ HRESULT *pResult,
            /* [out] */ ICLRPrivAssembly **ppAssembly);

public:

    HRESULT SetupBindingPaths(SString  &sTrustedPlatformAssemblies,
                              SString  &sPlatformResourceRoots,
                              SString  &sAppPaths,
                              SString  &sAppNiPaths);

    bool IsInTpaList(const SString  &sFileName);

    inline BINDER_SPACE::ApplicationContext *GetAppContext()
    {
        return &m_appContext;
    }

    HRESULT Bind(SString     &assemblyDisplayName,
                 LPCWSTR      wszCodeBase,
                 PEAssembly  *pParentAssembly,
                 BOOL         fNgenExplicitBind,
                 BOOL         fExplicitBindToNativeImage,
                 ICLRPrivAssembly **ppAssembly);

#ifndef CROSSGEN_COMPILE
    HRESULT PreBindByteArray(PEImage  *pPEImage, BOOL fInspectionOnly);
#endif // CROSSGEN_COMPILE

#if defined(FEATURE_HOST_ASSEMBLY_RESOLVER) && !defined(DACCESS_COMPILE) && !defined(CROSSGEN_COMPILE) && !defined(MDILNIGEN)
    HRESULT BindUsingPEImage( /* in */ PEImage *pPEImage, 
                              /* in */ BOOL fIsNativeImage, 
                              /* [retval][out] */ ICLRPrivAssembly **ppAssembly);
#endif // defined(FEATURE_HOST_ASSEMBLY_RESOLVER) && !defined(DACCESS_COMPILE) && !defined(CROSSGEN_COMPILE) && !defined(MDILNIGEN)

    HRESULT BindAssemblyByNameWorker(
            BINDER_SPACE::AssemblyName *pAssemblyName,
            BINDER_SPACE::Assembly **ppCoreCLRFoundAssembly,
            bool excludeAppPaths);

    INT_PTR GetManagedTPABinderInstance()
    {
        return m_ptrManagedAssemblyLoadContext;
    }

    void SetManagedTPABinderInstance(INT_PTR ptrManagedTPABinderInstance)
    {
        _ASSERTE(m_ptrManagedAssemblyLoadContext == NULL);

        m_ptrManagedAssemblyLoadContext = ptrManagedTPABinderInstance;
    }
    
    //=========================================================================
    // Internal implementation details
    //-------------------------------------------------------------------------
private:
    BINDER_SPACE::ApplicationContext m_appContext;

    INT_PTR m_ptrManagedAssemblyLoadContext;
};

#endif // __CLR_PRIV_BINDER_CORECLR_H__
