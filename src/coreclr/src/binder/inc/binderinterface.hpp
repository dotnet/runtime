//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//
// ============================================================
//
// BinderInterface.hpp
//


//
// Defines the public AssemblyBinder interface
//
// ============================================================

#ifndef __BINDER_INTERFACE_HPP__
#define __BINDER_INTERFACE_HPP__

class PEImage;
class PEAssembly;
class StringArrayList;

namespace BINDER_SPACE
{
    class Assembly;
    class AssemblyIdentityUTF8;
};

namespace BinderInterface
{
    HRESULT Init();

    HRESULT SetupContext(/* in */  LPCWSTR    wszApplicationBase,
                         /* in */  DWORD      dwAppDomainId,
                         /* out */ IUnknown **ppIApplicationContext);

    // See code:BINDER_SPACE::AssemblyBinder::GetAssembly for info on fNgenExplicitBind
    // and fExplicitBindToNativeImage, and see code:CEECompileInfo::LoadAssemblyByPath
    // for an example of how they're used.
    HRESULT Bind(/* in */ IUnknown                *pIApplicationContext,
                 /* in */ SString                 &assemblyDisplayName,
                 /* in */ LPCWSTR                  wszCodeBase,
                 /* in */ PEAssembly              *pParentAssembly,
                 /* in */ BOOL                     fNgenExplicitBind,
                 /* in */ BOOL                     fExplicitBindToNativeImage,
                 /*out */ BINDER_SPACE::Assembly **ppAssembly);

    //
    // Called via managed AppDomain.ExecuteAssembly variants and during binding host setup
    //
    HRESULT SetupBindingPaths(/* in */ IUnknown *pIApplicationContext,
                              /* in */ SString &sTrustedPlatformAssemblies,
                              /* in */ SString &sPlatformResourceRoots,
                              /* in */ SString &sAppPaths,
                              /* in */ SString &sAppNiPaths);
    
    //
    // Called via CoreAssemblySpec::BindToSystem
    //
    HRESULT BindToSystem(/* in */  SString                 &sSystemDirectory,
                         /* out */ BINDER_SPACE::Assembly **ppSystemAssembly,
                         /* in */  bool                     fBindToNativeImage);

#ifdef BINDER_DEBUG_LOG
    HRESULT Log(/* in */ LPCWSTR wszMessage);
#endif // BINDER_DEBUG_LOG

};

#endif
