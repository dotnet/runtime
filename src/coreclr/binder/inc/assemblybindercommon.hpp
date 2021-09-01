// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// ============================================================
//
// AssemblyBinder.hpp
//


//
// Defines the AssemblyBinder class
//
// ============================================================

#ifndef __BINDER__ASSEMBLY_BINDER_COMMON_HPP__
#define __BINDER__ASSEMBLY_BINDER_COMMON_HPP__

#include "bindertypes.hpp"
#include "bindresult.hpp"
#include "bundle.h"

class AssemblyBinder;
class DefaultAssemblyBinder;
class PEAssembly;
class PEImage;

namespace BINDER_SPACE
{
    class AssemblyIdentityUTF8;

    class AssemblyBinderCommon
    {
    public:
        static HRESULT BindAssembly(/* in */  AssemblyBinder      *pBinder, 
                                    /* in */  AssemblyName        *pAssemblyName,
                                    /* in */  LPCWSTR              szCodeBase,
                                    /* in */  PEAssembly          *pParentAssembly,
                                    /* in */  bool                 excludeAppPaths,
                                    /* out */ Assembly           **ppAssembly);

        static HRESULT BindToSystem(BINDER_SPACE::Assembly** ppSystemAssembly);

        static HRESULT BindToSystem(/* in */ SString    &systemDirectory,
                                    /* out */ Assembly **ppSystemAssembly);

        static HRESULT BindToSystemSatellite(/* in */ SString   &systemDirectory,
                                             /* in */ SString   &simpleName,
                                             /* in */ SString   &cultureName,
                                             /* out */ Assembly **ppSystemAssembly);

        static HRESULT GetAssembly(/* in */  SString     &assemblyPath,
                                   /* in */  BOOL         fIsInTPA,
                                   /* out */ Assembly   **ppAssembly,
                                   /* in */  LPCTSTR      szMDAssemblyPath = NULL,
                                   /* in */  BundleFileLocation bundleFileLocation = BundleFileLocation::Invalid());

#if !defined(DACCESS_COMPILE)
        static HRESULT BindUsingHostAssemblyResolver (/* in */ INT_PTR pManagedAssemblyLoadContextToBindWithin,
                                                      /* in */ AssemblyName       *pAssemblyName,
                                                      /* in */ DefaultAssemblyBinder *pTPABinder,
                                                      /* out */ Assembly           **ppAssembly);

        static HRESULT BindUsingPEImage(/* in */  AssemblyBinder     *pBinder,
                                        /* in */  BINDER_SPACE::AssemblyName *pAssemblyName,
                                        /* in */  PEImage            *pPEImage,
                                        /* in */  PEKIND              peKind,
                                        /* in */  IMDInternalImport  *pIMetaDataAssemblyImport,
                                        /* [retval] [out] */  Assembly **ppAssembly);
#endif // !defined(DACCESS_COMPILE)

        static HRESULT TranslatePEToArchitectureType(DWORD  *pdwPAFlags, PEKIND *PeKind);

        static HRESULT DefaultBinderSetupContext(DefaultAssemblyBinder** ppTPABinder);

        // TODO: The call indicates that this can come from a case where
        // pDomain->GetFusionContext() is null, hence this is static function
        // which handles a null binder. See if this actually happens
        static HRESULT GetAssemblyIdentity(LPCSTR     szTextualIdentity,
            BINDER_SPACE::ApplicationContext* pApplicationContext,
            NewHolder<BINDER_SPACE::AssemblyIdentityUTF8>& assemblyIdentityHolder);

    private:
        static HRESULT BindByName(/* in */  ApplicationContext *pApplicationContext,
                                  /* in */  AssemblyName       *pAssemblyName,
                                  /* in */  bool                skipFailureCaching,
                                  /* in */  bool                skipVersionCompatibilityCheck,
                                  /* in */  bool                excludeAppPaths,
                                  /* out */ BindResult         *pBindResult);

        static HRESULT BindWhereRef(/* in */  ApplicationContext *pApplicationContext,
                                    /* in */  PathString         &assemblyPath,
                                    /* in */  bool                excludeAppPaths,
                                    /* out */ BindResult         *pBindResult);

        static HRESULT BindLocked(/* in */  ApplicationContext *pApplicationContext,
                                  /* in */  AssemblyName       *pAssemblyName,
                                  /* in */  bool                skipVersionCompatibilityCheck,
                                  /* in */  bool                excludeAppPaths,
                                  /* out */ BindResult         *pBindResult);

        static HRESULT FindInExecutionContext(/* in */  ApplicationContext  *pApplicationContext,
                                              /* in */  AssemblyName        *pAssemblyName,
                                              /* out */ ContextEntry       **ppContextEntry);

        static HRESULT BindByTpaList(/* in */  ApplicationContext  *pApplicationContext,
                                     /* in */  AssemblyName        *pRequestedAssemblyName,
                                     /* in */  bool                 excludeAppPaths,
                                     /* out */ BindResult          *pBindResult);

        static HRESULT Register(/* in */  ApplicationContext *pApplicationContext,
                                /* in */  BindResult         *pBindResult);
        static HRESULT RegisterAndGetHostChosen(/* in */  ApplicationContext *pApplicationContext,
                                                /* in */  LONG                kContextVersion,
                                                /* in */  BindResult         *pBindResult,
                                                /* out */ BindResult         *pHostBindResult);

        static HRESULT OtherBindInterfered(/* in */ ApplicationContext *pApplicationContext,
                                           /* in */ BindResult         *pBindResult);
    };
};

#endif
