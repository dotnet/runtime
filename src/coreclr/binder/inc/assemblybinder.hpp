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

#ifndef __BINDER__ASSEMBLY_BINDER_HPP__
#define __BINDER__ASSEMBLY_BINDER_HPP__

#include "bindertypes.hpp"
#include "bindresult.hpp"
#include "bundle.h"

class CLRPrivBinderCoreCLR;
class PEAssembly;
class PEImage;

namespace BINDER_SPACE
{
    class AssemblyBinder
    {
    public:
        // See code:BINDER_SPACE::AssemblyBinder::GetAssembly for info on fNgenExplicitBind
        // and fExplicitBindToNativeImage, and see code:CEECompileInfo::LoadAssemblyByPath
        // for an example of how they're used.
        static HRESULT BindAssembly(/* in */  ApplicationContext  *pApplicationContext,
                                    /* in */  AssemblyName        *pAssemblyName,
                                    /* in */  LPCWSTR              szCodeBase,
                                    /* in */  PEAssembly          *pParentAssembly,
                                    /* in */  BOOL                 fNgenExplicitBind,
                                    /* in */  BOOL                 fExplicitBindToNativeImage,
                                    /* in */  bool                 excludeAppPaths,
                                    /* out */ Assembly           **ppAssembly);

        static HRESULT BindToSystem(/* in */ SString    &systemDirectory,
                                    /* out */ Assembly **ppSystemAssembly,
                                    /* in */ bool fBindToNativeImage);

        static HRESULT BindToSystemSatellite(/* in */ SString   &systemDirectory,
                                             /* in */ SString   &simpleName,
                                             /* in */ SString   &cultureName,
                                             /* out */ Assembly **ppSystemAssembly);

        static HRESULT GetAssembly(/* in */  SString     &assemblyPath,
                                   /* in */  BOOL         fIsInGAC,
                                   /* in */  BOOL         fExplicitBindToNativeImage,
                                   /* out */ Assembly   **ppAssembly,
                                   /* in */  LPCTSTR      szMDAssemblyPath = NULL,
                                   /* in */  BundleFileLocation bundleFileLocation = BundleFileLocation::Invalid());

#if !defined(DACCESS_COMPILE) && !defined(CROSSGEN_COMPILE)
        static HRESULT BindUsingHostAssemblyResolver (/* in */ INT_PTR pManagedAssemblyLoadContextToBindWithin,
                                                      /* in */ AssemblyName       *pAssemblyName,
                                                      /* in */ CLRPrivBinderCoreCLR *pTPABinder,
                                                      /* out */ Assembly           **ppAssembly);

        static HRESULT BindUsingPEImage(/* in */  ApplicationContext *pApplicationContext,
                                        /* in */  BINDER_SPACE::AssemblyName *pAssemblyName,
                                        /* in */  PEImage            *pPEImage,
                                        /* in */  PEKIND              peKind,
                                        /* in */  IMDInternalImport  *pIMetaDataAssemblyImport,
                                        /* [retval] [out] */  Assembly **ppAssembly);
#endif // !defined(DACCESS_COMPILE) && !defined(CROSSGEN_COMPILE)

        static HRESULT TranslatePEToArchitectureType(DWORD  *pdwPAFlags, PEKIND *PeKind);

    private:
        static HRESULT BindByName(/* in */  ApplicationContext *pApplicationContext,
                                  /* in */  AssemblyName       *pAssemblyName,
                                  /* in */  bool                skipFailureCaching,
                                  /* in */  bool                skipVersionCompatibilityCheck,
                                  /* in */  bool                excludeAppPaths,
                                  /* out */ BindResult         *pBindResult);

        // See code:BINDER_SPACE::AssemblyBinder::GetAssembly for info on fNgenExplicitBind
        // and fExplicitBindToNativeImage, and see code:CEECompileInfo::LoadAssemblyByPath
        // for an example of how they're used.
        static HRESULT BindWhereRef(/* in */  ApplicationContext *pApplicationContext,
                                    /* in */  PathString         &assemblyPath,
                                    /* in */  BOOL                fNgenExplicitBind,
                                    /* in */  BOOL                fExplicitBindToNativeImage,
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
