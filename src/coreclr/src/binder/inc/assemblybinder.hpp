// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
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
#include "coreclrbindercommon.h"

class CLRPrivBinderAssemblyLoadContext;
class CLRPrivBinderCoreCLR;

namespace BINDER_SPACE
{
    typedef enum
    {
        kBindingStoreGAC      = 0x01,
        kBindingStoreManifest = 0x02,
        kBindingStoreHost     = 0x04,
        kBindingStoreContext  = 0x08
    } BindingStore;

    class AssemblyBinder
    {
    public:
        static HRESULT Startup();

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

        static HRESULT GetAssemblyFromImage(/* in */ PEImage    *pPEImage,
                                            /* in */ PEImage    *pNativePEImage,
                                            /* out */ Assembly **ppAssembly);

        // Special assembly binder entry point for byte arrays
        static HRESULT PreBindByteArray(/* in */  ApplicationContext *pApplicationContext,
                                        /* in */  PEImage            *pPEImage,
                                        /* in */  BOOL                fInspectionOnly);

        static HRESULT GetAssembly(/* in */  SString     &assemblyPath,
                                   /* in */  BOOL         fInspectionOnly,
                                   /* in */  BOOL         fIsInGAC,
                                   /* in */  BOOL         fExplicitBindToNativeImage,
                                   /* out */ Assembly   **ppAssembly,
                                   /* in */  LPCTSTR      szMDAssemblyPath = NULL);

#if !defined(DACCESS_COMPILE) && !defined(CROSSGEN_COMPILE)
        static HRESULT BindUsingHostAssemblyResolver (/* in */ INT_PTR pManagedAssemblyLoadContextToBindWithin,
                                                      /* in */ AssemblyName       *pAssemblyName,
                                                      /* in */ IAssemblyName      *pIAssemblyName,
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
        
    protected:
        enum
        {
            BIND_NONE = 0x00,
            BIND_CACHE_FAILURES = 0x01,
            BIND_CACHE_RERUN_BIND = 0x02,
            BIND_IGNORE_DYNAMIC_BINDS = 0x04
#if !defined(DACCESS_COMPILE) && !defined(CROSSGEN_COMPILE)
            ,
            BIND_IGNORE_REFDEF_MATCH = 0x8
#endif // !defined(DACCESS_COMPILE) && !defined(CROSSGEN_COMPILE)
        };

        static BOOL IgnoreDynamicBinds(DWORD dwBindFlags)
        {
            return ((dwBindFlags & BIND_IGNORE_DYNAMIC_BINDS) != 0);
        }

        static BOOL CacheBindFailures(DWORD dwBindFlags)
        {
            return ((dwBindFlags & BIND_CACHE_FAILURES) != 0);
        }

        static BOOL RerunBind(DWORD dwBindFlags)
        {
            return ((dwBindFlags & BIND_CACHE_RERUN_BIND) != 0);
        }
        
#if !defined(DACCESS_COMPILE) && !defined(CROSSGEN_COMPILE)
        static BOOL IgnoreRefDefMatch(DWORD dwBindFlags)
        {
            return ((dwBindFlags & BIND_IGNORE_REFDEF_MATCH) != 0);
        }
#endif // !defined(DACCESS_COMPILE) && !defined(CROSSGEN_COMPILE)
        
        static HRESULT BindByName(/* in */  ApplicationContext *pApplicationContext,
                                  /* in */  AssemblyName       *pAssemblyName,
                                  /* in */  DWORD               dwBindFlags,
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
                                  /* in */  DWORD               dwBindFlags,
                                  /* in */  bool                excludeAppPaths,
                                  /* out */ BindResult         *pBindResult);
        static HRESULT BindLockedOrService(/* in */  ApplicationContext *pApplicationContext,
                                           /* in */  AssemblyName       *pAssemblyName,
                                           /* in */  bool                excludeAppPaths,
                                           /* out */ BindResult         *pBindResult);

        static HRESULT FindInExecutionContext(/* in */  ApplicationContext  *pApplicationContext,
                                              /* in */  AssemblyName        *pAssemblyName,
                                              /* out */ ContextEntry       **ppContextEntry);

        static HRESULT BindByTpaList(/* in */  ApplicationContext  *pApplicationContext,
                                     /* in */  AssemblyName        *pRequestedAssemblyName,
                                     /* in */  BOOL                 fInspectionOnly,
                                     /* in */  bool                 excludeAppPaths,
                                     /* out */ BindResult          *pBindResult);
        
        static HRESULT Register(/* in */  ApplicationContext *pApplicationContext,
                                /* in */  BOOL                fInspectionOnly,
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
