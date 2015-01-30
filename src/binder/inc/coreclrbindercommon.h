//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//


#ifndef __CORECLR_BINDER_COMMON_H__
#define __CORECLR_BINDER_COMMON_H__

#include "clrprivbinding.h"
#include "internalunknownimpl.h"
#include "applicationcontext.hpp"


namespace BINDER_SPACE
{
    class AssemblyIdentityUTF8;
};

class CLRPrivBinderCoreCLR;
    
// General purpose AssemblyBinder helper class
class CCoreCLRBinderHelper
{
public:
    static HRESULT Init();
    
    static HRESULT DefaultBinderSetupContext(DWORD      dwAppDomainId,
                                CLRPrivBinderCoreCLR **ppTPABinder);
                                
    // ABHI-TODO: The call indicates that this can come from a case where
    // pDomain->GetFusionContext() is null, hence this is static function
    // which handles a null binder. See if this actually happens
    static HRESULT GetAssemblyIdentity(LPCSTR     szTextualIdentity,
                                       BINDER_SPACE::ApplicationContext  *pApplicationContext,
                                       NewHolder<BINDER_SPACE::AssemblyIdentityUTF8> &assemblyIdentityHolder);
    
    //=============================================================================
    // Class functions that provides binding services beyond the ICLRPrivInterface
    //-----------------------------------------------------------------------------
    static HRESULT BindToSystem(ICLRPrivAssembly **ppSystemAssembly, bool fBindToNativeImage);

    static HRESULT BindToSystemSatellite(SString            &systemPath,
                                         SString           &sSimpleName,
                                         SString           &sCultureName,
                                         ICLRPrivAssembly **ppSystemAssembly);
                                         
    static HRESULT GetAssemblyFromImage(PEImage   *pPEImage,
                                        PEImage   *pNativePEImage,
                                        ICLRPrivAssembly **ppAssembly);

    static HRESULT GetAssembly(/* in */  SString     &assemblyPath,
                                   /* in */  BOOL         fInspectionOnly,
                                   /* in */  BOOL         fIsInGAC,
                                   /* in */  BOOL         fExplicitBindToNativeImage,
                                   /* out */ BINDER_SPACE::Assembly   **ppAssembly);
};

#endif // __CORECLR_BINDER_COMMON_H__
