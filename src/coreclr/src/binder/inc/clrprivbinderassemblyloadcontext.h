// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


#ifndef __CLRPRIVBINDERASSEMBLYLOADCONTEXT_H__
#define __CLRPRIVBINDERASSEMBLYLOADCONTEXT_H__

#include "coreclrbindercommon.h"
#include "applicationcontext.hpp"
#include "clrprivbindercoreclr.h"

#if defined(FEATURE_HOST_ASSEMBLY_RESOLVER) && !defined(DACCESS_COMPILE) && !defined(CROSSGEN_COMPILE)

namespace BINDER_SPACE
{
    class AssemblyIdentityUTF8;
};

class CLRPrivBinderAssemblyLoadContext : public IUnknownCommon<ICLRPrivBinder>
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
    //=========================================================================
    // Class functions
    //-------------------------------------------------------------------------

    static HRESULT SetupContext(DWORD      dwAppDomainId, CLRPrivBinderCoreCLR *pTPABinder, 
                                UINT_PTR ptrAssemblyLoadContext, CLRPrivBinderAssemblyLoadContext **ppBindContext);
                    
    CLRPrivBinderAssemblyLoadContext();
    
    inline BINDER_SPACE::ApplicationContext *GetAppContext()
    {
        return &m_appContext;
    }
    
    inline INT_PTR GetManagedAssemblyLoadContext()
    {
        return m_ptrManagedAssemblyLoadContext;
    }

    HRESULT BindUsingPEImage( /* in */ PEImage *pPEImage, 
                              /* in */ BOOL fIsNativeImage, 
                              /* [retval][out] */ ICLRPrivAssembly **ppAssembly);
                              
    //=========================================================================
    // Internal implementation details
    //-------------------------------------------------------------------------
private:
    HRESULT BindAssemblyByNameWorker(BINDER_SPACE::AssemblyName *pAssemblyName, BINDER_SPACE::Assembly **ppCoreCLRFoundAssembly);
            
    BINDER_SPACE::ApplicationContext m_appContext;    
    
    CLRPrivBinderCoreCLR *m_pTPABinder;
    
    INT_PTR m_ptrManagedAssemblyLoadContext;
};

#endif // defined(FEATURE_HOST_ASSEMBLY_RESOLVER) && !defined(DACCESS_COMPILE) && !defined(CROSSGEN_COMPILE)
#endif // __CLRPRIVBINDERASSEMBLYLOADCONTEXT_H__
